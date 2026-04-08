"""Asset worker: Kafka consumer and event processor."""
import json
import logging
import signal
import sys
from datetime import datetime
from typing import Any

from config.settings import Settings
from domains.asset.constants import AssetConstants
from messaging.consumer.kafka_consumer import AssetEventConsumer
from messaging.provider.kafka_producer import AssetEventProducer
from pipelines.asset_dto import (
    AssetCreatedEventDto,
    AssetHealthUpdatedEventDto,
    SimulationStateUpdatedEventDto,
)
from pipelines.asset_pipeline import (
    asset_state_to_dto,
    build_alert_event,
    build_effective_schema,
    calculate_derived_properties,
    calculate_state,
)
from pipelines.recommendation_pipeline import (
    build_trend_results,
    generate_recommendations,
    recommendation_to_event_payload,
)
from repositories.mongo.asset_repository import AssetRepository
from repositories.mongo.object_type_repository import ObjectTypeRepository

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)


class AssetWorker:
    """Asset event processing worker."""

    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or Settings()
        self.consumer = AssetEventConsumer(self.settings)
        self.producer = AssetEventProducer(self.settings)
        self.repository = AssetRepository(self.settings)
        self.object_type_repository = ObjectTypeRepository(self.settings)
        self.running = True

    def process_asset_created(self, event: AssetCreatedEventDto) -> None:
        """Process asset.created event."""
        logger.info(f"Processing asset.created: {event.asset_id}")
        payload = event.payload

        self.repository.save_asset(
            asset_id=event.asset_id,
            asset_type=payload.get("type", "unknown"),
            connections=payload.get("connections", []),
            metadata=payload.get("metadata", {}),
        )

        # Save raw event
        self.repository.save_event(
            asset_id=event.asset_id,
            event_type=event.event_type,
            timestamp=event.timestamp,
            payload=payload,
        )

    def process_health_updated(self, event: AssetHealthUpdatedEventDto) -> None:
        """Process asset.health.updated event."""
        logger.info(f"Processing asset.health.updated: {event.asset_id}")

        asset_doc = self.repository.get_asset(event.asset_id) or {}
        asset_type = asset_doc.get("type", "unknown") if isinstance(asset_doc, dict) else "unknown"
        schema = self.object_type_repository.get_by_object_type(str(asset_type))
        metadata = asset_doc.get("metadata") if isinstance(asset_doc, dict) else None
        if not isinstance(metadata, dict):
            metadata = {}
        effective_schema = build_effective_schema(schema, metadata)

        state = calculate_state(event, asset_type=str(asset_type), schema=effective_schema)
        state_dto = asset_state_to_dto(state)

        # Save state
        self.repository.save_state(state_dto)

        # Save raw event
        self.repository.save_event(
            asset_id=event.asset_id,
            event_type=event.event_type,
            timestamp=event.timestamp,
            payload=event.payload,
        )

        if state.status in (AssetConstants.Status.WARNING, AssetConstants.Status.ERROR):
            run_id = event.payload.get("runId")
            alert_payload = build_alert_event(
                asset_id=state.asset_id,
                timestamp=event.timestamp,
                status=state.status,
                properties=state.properties,
                run_id=run_id,
            )
            self.producer.send(self.settings.kafka_topic_alert_events, value=alert_payload)

        self._generate_recommendations(state.asset_id, event.payload.get("type", "asset"))

    def process_simulation_state_updated(self, event: SimulationStateUpdatedEventDto) -> None:
        """Process simulation.state.updated event (backend propagation)."""
        logger.info(f"Processing simulation.state.updated: {event.asset_id}")

        asset_doc = self.repository.get_asset(event.asset_id) or {}
        asset_type = asset_doc.get("type", "unknown") if isinstance(asset_doc, dict) else "unknown"
        schema = self.object_type_repository.get_by_object_type(str(asset_type))
        metadata = asset_doc.get("metadata") if isinstance(asset_doc, dict) else None
        if not isinstance(metadata, dict):
            metadata = {}
        effective_schema = build_effective_schema(schema, metadata)

        payload = dict(event.payload)
        simulation_status = payload.get("simulationStatus")
        if simulation_status is not None:
            payload.pop("status", None)
        props = payload.get("properties")
        if not isinstance(props, dict):
            props = {}
        merged_props = dict(props)
        if effective_schema:
            delta_seconds = float(payload.get("deltaSeconds", 1.0))
            merged_props = {
                **props,
                **calculate_derived_properties(dict(props), effective_schema, delta_seconds),
            }
        payload["properties"] = merged_props

        event_merged = event.model_copy(update={"payload": payload})
        state = calculate_state(event_merged, asset_type=str(asset_type), schema=effective_schema)

        sim_s = simulation_status if isinstance(simulation_status, str) else None
        state_dto = asset_state_to_dto(state, simulation_status=sim_s)
        self.repository.save_state(state_dto)
        self.repository.save_event(
            asset_id=event.asset_id,
            event_type=event.event_type,
            timestamp=event.timestamp,
            payload=event.payload,
        )

        if state.status in (AssetConstants.Status.WARNING, AssetConstants.Status.ERROR):
            run_id = event.payload.get("runId")
            alert_payload = build_alert_event(
                asset_id=state.asset_id,
                timestamp=event.timestamp,
                status=state.status,
                properties=state.properties,
                run_id=run_id,
            )
            self.producer.send(self.settings.kafka_topic_alert_events, value=alert_payload)

        self._generate_recommendations(state.asset_id, event.payload.get("type", "asset"))

    def process_event(self, event: dict) -> None:
        """Process event based on event type."""
        event_type = event.get("eventType")

        try:
            if event_type == AssetConstants.EventType.ASSET_CREATED:
                event_dto = AssetCreatedEventDto(**event)
                self.process_asset_created(event_dto)
            elif event_type == AssetConstants.EventType.ASSET_HEALTH_UPDATED:
                event_dto = AssetHealthUpdatedEventDto(**event)
                self.process_health_updated(event_dto)
            elif event_type == AssetConstants.EventType.SIMULATION_STATE_UPDATED:
                event_dto = SimulationStateUpdatedEventDto(**event)
                self.process_simulation_state_updated(event_dto)
            elif event_type in (
                AssetConstants.EventType.SIMULATION_TICK_STARTED,
                AssetConstants.EventType.SIMULATION_TICK_COMPLETED,
            ):
                logger.info(
                    "Tick envelope %s runId=%s payload=%s",
                    event_type,
                    event.get("runId"),
                    event.get("payload"),
                )
            elif event_type == AssetConstants.EventType.RECOMMENDATION_APPLIED:
                self.process_recommendation_applied(event)
            else:
                logger.warning(f"Unknown event type: {event_type}")
        except Exception as e:
            logger.error(f"Error processing event: {e}", exc_info=True)

    def run(self) -> None:
        """Run the worker (consume and process events)."""
        logger.info("Starting asset worker...")
        logger.info(f"Kafka: {self.settings.kafka_bootstrap_servers}")
        logger.info(f"Asset topic: {self.settings.kafka_topic_asset_events}")
        logger.info(f"Alert topic: {self.settings.kafka_topic_alert_events}")
        logger.info(f"MongoDB: {self.settings.mongodb_database}")

        try:
            for event in self.consumer.consume():
                if not self.running:
                    break
                self.process_event(event)
        except KeyboardInterrupt:
            logger.info("Received interrupt signal, shutting down...")
        except Exception as e:
            logger.error(f"Worker error: {e}", exc_info=True)
            raise
        finally:
            self.shutdown()

    def shutdown(self) -> None:
        """Shutdown worker and close connections."""
        logger.info("Shutting down asset worker...")
        self.running = False
        self.consumer.close()
        self.producer.close()
        self.repository.close()
        self.object_type_repository.close()

    def signal_handler(self, signum: int, frame: Any) -> None:
        """Handle shutdown signals."""
        logger.info(f"Received signal {signum}, shutting down...")
        self.running = False

    def _generate_recommendations(self, asset_id: str, object_type: str) -> None:
        keys = ["temperature", "power", "efficiency", "charge", "throughput"]
        series = self.repository.get_recent_property_series(asset_id=asset_id, keys=keys)
        thresholds = {
            "temperature": 0.0,
            "power": 250.0,
            "efficiency": 80.0,
            "charge": 10.0,
            "throughput": 50.0,
        }
        trends = build_trend_results(asset_id, object_type, series, thresholds)
        recommendations = generate_recommendations(trends)
        for rec in recommendations:
            doc = {
                "recommendationId": rec.id,
                "objectId": rec.object_id,
                "objectType": rec.object_type,
                "severity": rec.severity,
                "category": rec.category,
                "title": rec.title,
                "description": rec.description,
                "suggestedAction": rec.suggested_action,
                "analysisBasis": rec.analysis_basis,
                "status": rec.status,
                "createdAt": rec.created_at,
                "updatedAt": rec.updated_at,
            }
            self.repository.save_recommendation(doc)
            payload = recommendation_to_event_payload(rec)
            self.producer.send(self.settings.kafka_topic_recommendation_events, value=payload)

    def process_recommendation_applied(self, event: dict[str, Any]) -> None:
        payload = event.get("payload", {})
        if not isinstance(payload, dict):
            return
        recommendation_id = payload.get("recommendationId")
        run_id = payload.get("runId") or event.get("runId")
        if not recommendation_id or not run_id:
            return
        expected_change = payload.get("patch")
        if not isinstance(expected_change, dict):
            expected_change = {}
        self.repository.mark_recommendation_applied(
            recommendation_id=recommendation_id,
            run_id=str(run_id),
            expected_change=expected_change,
        )


def main() -> None:
    """Main entry point."""
    worker = AssetWorker()

    # Register signal handlers
    signal.signal(signal.SIGINT, worker.signal_handler)
    signal.signal(signal.SIGTERM, worker.signal_handler)

    try:
        worker.run()
    except Exception as e:
        logger.error(f"Fatal error: {e}", exc_info=True)
        sys.exit(1)


if __name__ == "__main__":
    main()
