"""Asset worker: Kafka consumer and event processor."""
import json
import logging
import signal
import sys
from datetime import datetime

from config.settings import Settings
from domains.asset.constants import AssetConstants
from messaging.consumer.kafka_consumer import AssetEventConsumer
from pipelines.asset_dto import AssetCreatedEventDto, AssetHealthUpdatedEventDto
from pipelines.asset_pipeline import asset_state_to_dto, calculate_state
from repositories.mongo.asset_repository import AssetRepository

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
        self.repository = AssetRepository(self.settings)
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

        # Calculate state from event
        state = calculate_state(event)
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
            else:
                logger.warning(f"Unknown event type: {event_type}")
        except Exception as e:
            logger.error(f"Error processing event: {e}", exc_info=True)

    def run(self) -> None:
        """Run the worker (consume and process events)."""
        logger.info("Starting asset worker...")
        logger.info(f"Kafka: {self.settings.kafka_bootstrap_servers}")
        logger.info(f"Topic: {self.settings.kafka_topic_asset_events}")
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
        self.repository.close()

    def signal_handler(self, signum: int, frame: Any) -> None:
        """Handle shutdown signals."""
        logger.info(f"Received signal {signum}, shutting down...")
        self.running = False


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
