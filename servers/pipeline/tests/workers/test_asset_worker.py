"""Tests for asset worker: alert publish on WARNING/ERROR."""
from datetime import datetime, timezone
from unittest.mock import MagicMock

import pytest

from domains.asset.constants import AssetConstants
from pipelines.asset_dto import AssetHealthUpdatedEventDto, SimulationStateUpdatedEventDto
from workers.asset_worker import AssetWorker


class TestAssetWorkerAlertPublish:
    """Worker publishes alert.generated to Kafka only when state is WARNING or ERROR."""

    def test_process_health_updated_publishes_alert_when_warning(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()

        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -8, "power": 100},
        )
        worker.process_health_updated(event)

        worker.producer.send.assert_called_once()
        call_args = worker.producer.send.call_args
        assert call_args[0][0] == worker.settings.kafka_topic_asset_events
        value = call_args[1]["value"]
        assert value["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert value["assetId"] == "freezer-1"
        assert value["payload"]["severity"] == "warning"
        assert value["payload"]["message"] == "Asset state: warning"

    def test_process_health_updated_does_not_publish_when_normal(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()

        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -15, "power": 100},
        )
        worker.process_health_updated(event)

        worker.producer.send.assert_not_called()

    def test_process_simulation_state_updated_publishes_alert_when_error(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()

        event = SimulationStateUpdatedEventDto(
            eventType="simulation.state.updated",
            assetId="conveyor-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": 5, "power": 100, "runId": "run-456"},
        )
        worker.process_simulation_state_updated(event)

        worker.producer.send.assert_called_once()
        value = worker.producer.send.call_args[1]["value"]
        assert value["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert value["payload"]["severity"] == "error"
        assert value["payload"]["metadata"] == {"runId": "run-456"}
