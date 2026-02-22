"""Tests for asset pipeline: state calculation."""
from datetime import datetime, timezone

import pytest

from domains.asset import AssetConstants, AssetState, AssetStatus
from pipelines.asset_dto import AssetHealthUpdatedEventDto, AssetStateDto
from pipelines.asset_pipeline import asset_state_to_dto, build_alert_event, calculate_state


class TestCalculateState:
    """State calculation from health updated events."""

    def test_normal_status_when_temperature_low(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -15, "power": 100},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.NORMAL
        assert state.current_temp == -15
        assert state.current_power == 100

    def test_warning_status_when_temperature_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -8, "power": 100},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_error_status_when_temperature_above_zero(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": 5, "power": 100},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_warning_status_when_power_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -15, "power": 220},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_error_status_when_power_very_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -15, "power": 260},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_error_takes_precedence_over_warning(self) -> None:
        """Error status should take precedence over warning."""
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": 5, "power": 220},  # Both error and warning conditions
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_uses_event_status_if_provided(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -15, "power": 100, "status": "warning"},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_extracts_metadata_excluding_known_fields(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={
                "temperature": -5,
                "power": 120,
                "humidity": 45,
                "vibration": 0.5,
            },
        )
        state = calculate_state(event)
        assert "humidity" in state.metadata
        assert "vibration" in state.metadata
        assert state.metadata["humidity"] == 45
        assert state.metadata["vibration"] == 0.5
        assert "temperature" not in state.metadata
        assert "power" not in state.metadata
        assert "status" not in state.metadata

    def test_handles_missing_temperature(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"power": 120},
        )
        state = calculate_state(event)
        assert state.current_temp is None
        assert state.current_power == 120

    def test_handles_missing_power(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -5},
        )
        state = calculate_state(event)
        assert state.current_temp == -5
        assert state.current_power is None

    def test_sets_last_event_type(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -5},
        )
        state = calculate_state(event)
        assert state.last_event_type == "asset.health.updated"

    def test_sets_updated_at_from_event_timestamp(self) -> None:
        timestamp = datetime.now(timezone.utc)
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=timestamp,
            payload={"temperature": -5},
        )
        state = calculate_state(event)
        assert state.updated_at == timestamp


class TestAssetStateToDto:
    """Conversion from AssetState to AssetStateDto."""

    def test_converts_all_fields(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            current_temp=-5.0,
            current_power=120.0,
            status=AssetConstants.Status.WARNING,
            last_event_type="asset.health.updated",
            updated_at=datetime.now(timezone.utc),
            metadata={"humidity": 45},
        )
        dto = asset_state_to_dto(state)
        assert isinstance(dto, AssetStateDto)
        assert dto.asset_id == "freezer-1"
        assert dto.current_temp == -5.0
        assert dto.current_power == 120.0
        assert dto.status == AssetConstants.Status.WARNING
        assert dto.last_event_type == "asset.health.updated"
        assert dto.metadata["humidity"] == 45

    def test_handles_none_values(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
            current_temp=None,
            current_power=None,
        )
        dto = asset_state_to_dto(state)
        assert dto.current_temp is None
        assert dto.current_power is None


class TestBuildAlertEvent:
    """Alert event payload for Kafka (WARNING/ERROR only)."""

    def test_warning_status_maps_to_severity_warning(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="freezer-1",
            timestamp=ts,
            status=AssetConstants.Status.WARNING,
            current_temp=-5.0,
            current_power=None,
        )
        assert out["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert out["assetId"] == "freezer-1"
        assert out["timestamp"] == ts.isoformat()
        assert out["payload"]["severity"] == "warning"
        assert out["payload"]["message"] == "Asset state: warning"
        assert out["payload"]["metric"] == "temperature"
        assert out["payload"]["current"] == -5.0
        assert out["payload"]["threshold"] == -10
        assert out["payload"]["code"] == "TEMP_HIGH"

    def test_error_status_maps_to_severity_error(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="conveyor-1",
            timestamp=ts,
            status=AssetConstants.Status.ERROR,
            current_temp=None,
            current_power=260.0,
        )
        assert out["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert out["assetId"] == "conveyor-1"
        assert out["payload"]["severity"] == "error"
        assert out["payload"]["message"] == "Asset state: error"
        assert out["payload"]["metric"] == "power"
        assert out["payload"]["current"] == 260.0
        assert out["payload"]["threshold"] == 250
        assert out["payload"]["code"] == "POWER_OVERLOAD"

    def test_run_id_in_metadata_when_provided(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="freezer-1",
            timestamp=ts,
            status=AssetConstants.Status.WARNING,
            run_id="run-123",
        )
        assert out["payload"]["metadata"] == {"runId": "run-123"}
        assert out["payload"]["severity"] == "warning"
        assert out["payload"]["message"] == "Asset state: warning"
