"""Tests for asset pipeline: state calculation."""
from datetime import datetime, timezone

from domains.asset import AssetConstants, AssetState
from pipelines.asset_dto import (
    AssetHealthUpdatedEventDto,
    AssetStateDto,
    SimulationStateUpdatedEventDto,
)
from pipelines.asset_pipeline import (
    asset_state_to_dto,
    build_alert_event,
    build_effective_schema,
    calculate_derived_properties,
    calculate_state,
)

# Minimal Drone-like schema (matches seeds/drone_objecttype.json thresholds + derived battery).
_DRONE_SCHEMA = {
    "ownProperties": [
        {
            "key": "battery_level",
            "simulationBehavior": "Derived",
            "baseValue": 100,
            "constraints": {"min": 0, "max": 100},
            "derivedRule": {
                "type": "linear",
                "timeUnit": "hour",
                "inputs": [{"property": "power_draw", "coefficient": -1.0}],
            },
            "alertThresholds": [
                {"level": "warning", "condition": "lt", "value": 20},
                {"level": "error", "condition": "lt", "value": 10},
            ],
        },
        {
            "key": "power_draw",
            "simulationBehavior": "Settable",
            "baseValue": 0.5,
        },
    ]
}


class TestCalculateState:
    """State calculation from health updated events."""

    def test_normal_status_when_temperature_low(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -15, "power": 100}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.NORMAL
        assert state.properties["temperature"] == -15
        assert state.properties["power"] == 100

    def test_warning_status_when_temperature_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -8, "power": 100}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_error_status_when_temperature_above_zero(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": 5, "power": 100}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_warning_status_when_power_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -15, "power": 220}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_error_status_when_power_very_high(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -15, "power": 260}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_error_takes_precedence_over_warning(self) -> None:
        """Error status should take precedence over warning."""
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": 5, "power": 220}},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.ERROR

    def test_uses_event_status_if_provided(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -15, "power": 100}, "status": "warning"},
        )
        state = calculate_state(event)
        assert state.status == AssetConstants.Status.WARNING

    def test_extracts_metadata_excluding_known_fields(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={
                "properties": {"temperature": -5, "power": 120, "humidity": 45},
                "runId": "run-1",
            },
        )
        state = calculate_state(event)
        assert state.properties["humidity"] == 45
        assert state.metadata["runId"] == "run-1"
        assert "status" not in state.metadata

    def test_handles_missing_temperature(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"power": 120}},
        )
        state = calculate_state(event)
        assert "temperature" not in state.properties
        assert state.properties["power"] == 120

    def test_handles_missing_power(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"temperature": -5}},
        )
        state = calculate_state(event)
        assert state.properties["temperature"] == -5
        assert "power" not in state.properties

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
            payload={"properties": {"temperature": -5}},
        )
        state = calculate_state(event)
        assert state.updated_at == timestamp

    def test_schema_alert_threshold_battery_warning(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="drone-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"battery_level": 15, "power_draw": 0.5}},
        )
        state = calculate_state(event, asset_type="Drone", schema=_DRONE_SCHEMA)
        assert state.status == AssetConstants.Status.WARNING

    def test_schema_alert_threshold_battery_error(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="drone-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"battery_level": 8, "power_draw": 0.5}},
        )
        state = calculate_state(event, asset_type="Drone", schema=_DRONE_SCHEMA)
        assert state.status == AssetConstants.Status.ERROR

    def test_explicit_status_overrides_schema_thresholds(self) -> None:
        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="drone-1",
            timestamp=datetime.now(timezone.utc),
            payload={
                "properties": {"battery_level": 5, "power_draw": 0.5},
                "status": "normal",
            },
        )
        state = calculate_state(event, asset_type="Drone", schema=_DRONE_SCHEMA)
        assert state.status == AssetConstants.Status.NORMAL


class TestCalculateDerivedProperties:
    """Linear derivedRule updates from ObjectType payload."""

    def test_linear_hour_drain(self) -> None:
        current = {"battery_level": 100.0, "power_draw": 0.5}
        out = calculate_derived_properties(current, _DRONE_SCHEMA, 3600.0)
        assert out["battery_level"] == 99.5


_EXTRA_ONLY_DERIVED = [
    {
        "key": "custom_metric",
        "simulationBehavior": "Derived",
        "baseValue": 10,
        "constraints": {"min": 0, "max": 100},
        "derivedRule": {
            "type": "linear",
            "timeUnit": "second",
            "inputs": [{"property": "input_x", "coefficient": 1.0}],
        },
        "alertThresholds": [
            {"level": "error", "condition": "gt", "value": 50},
        ],
    },
    {"key": "input_x", "simulationBehavior": "Settable", "baseValue": 2},
]


class TestBuildEffectiveSchema:
    """Schema + metadata.extraProperties merge (extra wins on duplicate keys)."""

    def test_metadata_only_extras_feed_derived_and_thresholds(self) -> None:
        eff = build_effective_schema(None, {"extraProperties": _EXTRA_ONLY_DERIVED})
        assert eff is not None
        assert len(eff["ownProperties"]) == 2

        out = calculate_derived_properties(
            {"custom_metric": 10.0, "input_x": 5.0}, eff, 1.0
        )
        assert out["custom_metric"] == 15.0

        event = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="extra-1",
            timestamp=datetime.now(timezone.utc),
            payload={"properties": {"custom_metric": 55, "input_x": 1}},
        )
        state = calculate_state(event, asset_type="Custom", schema=eff)
        assert state.status == AssetConstants.Status.ERROR

    def test_duplicate_key_extra_overrides_schema(self) -> None:
        schema = {
            "ownProperties": [
                {"key": "x", "simulationBehavior": "Settable", "baseValue": 1},
            ]
        }
        metadata = {
            "extraProperties": [
                {
                    "key": "x",
                    "simulationBehavior": "Derived",
                    "baseValue": 0,
                    "derivedRule": {
                        "type": "linear",
                        "timeUnit": "second",
                        "inputs": [{"property": "y", "coefficient": 1.0}],
                    },
                },
                {"key": "y", "simulationBehavior": "Settable", "baseValue": 3},
            ]
        }
        eff = build_effective_schema(schema, metadata)
        assert eff is not None
        assert len(eff["ownProperties"]) == 2
        x_def = next(p for p in eff["ownProperties"] if p.get("key") == "x")
        assert x_def.get("simulationBehavior") == "Derived"

    def test_resolved_properties_preferred_over_own(self) -> None:
        schema = {
            "ownProperties": [{"key": "a", "simulationBehavior": "Settable", "baseValue": 1}],
            "resolvedProperties": [
                {"key": "b", "simulationBehavior": "Settable", "baseValue": 2}
            ],
        }
        eff = build_effective_schema(schema, {})
        assert eff is not None
        keys = {p["key"] for p in eff["ownProperties"]}
        assert keys == {"b"}


class TestSimulationStateWithSchema:
    """Merged derived properties then status from same path as worker."""

    def test_merged_battery_then_threshold(self) -> None:
        ts = datetime.now(timezone.utc)
        payload: dict = {
            "properties": {"battery_level": 100.0, "power_draw": 0.5},
            "deltaSeconds": 3600.0,
        }
        props = dict(payload["properties"])
        merged = {**props, **calculate_derived_properties(props, _DRONE_SCHEMA, 3600.0)}
        payload["properties"] = merged

        event = SimulationStateUpdatedEventDto(
            eventType="simulation.state.updated",
            assetId="drone-1",
            timestamp=ts,
            payload=payload,
        )
        state = calculate_state(event, asset_type="Drone", schema=_DRONE_SCHEMA)
        assert state.properties["battery_level"] == 99.5
        assert state.status == AssetConstants.Status.NORMAL


class TestAssetStateToDto:
    """Conversion from AssetState to AssetStateDto."""

    def test_converts_all_fields(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            properties={"temperature": -5.0, "power": 120.0},
            status=AssetConstants.Status.WARNING,
            last_event_type="asset.health.updated",
            updated_at=datetime.now(timezone.utc),
            metadata={"humidity": 45},
        )
        dto = asset_state_to_dto(state)
        assert isinstance(dto, AssetStateDto)
        assert dto.asset_id == "freezer-1"
        assert dto.properties["temperature"] == -5.0
        assert dto.properties["power"] == 120.0
        assert dto.status == AssetConstants.Status.WARNING
        assert dto.operational_status == AssetConstants.Status.WARNING
        assert dto.last_event_type == "asset.health.updated"
        assert dto.metadata["humidity"] == 45

    def test_handles_none_values(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
            properties={},
        )
        dto = asset_state_to_dto(state)
        assert dto.properties == {}


class TestBuildAlertEvent:
    """Alert event payload for Kafka (WARNING/ERROR only)."""

    def test_warning_status_maps_to_severity_warning(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="freezer-1",
            timestamp=ts,
            status=AssetConstants.Status.WARNING,
            properties={"temperature": -5.0},
        )
        assert out["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert out["assetId"] == "freezer-1"
        assert out["timestamp"] == ts.isoformat()
        assert out["schemaVersion"] == "v1"
        assert "runId" not in out
        assert out["payload"]["severity"] == "warning"
        assert out["payload"]["message"] == "Asset state: warning"
        assert out["payload"]["metrics"][0]["metric"] == "temperature"
        assert out["payload"]["metrics"][0]["current"] == -5.0
        assert out["payload"]["metrics"][0]["threshold"] == -10
        assert out["payload"]["metrics"][0]["code"] == "TEMP_HIGH"

    def test_error_status_maps_to_severity_error(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="conveyor-1",
            timestamp=ts,
            status=AssetConstants.Status.ERROR,
            properties={"power": 260.0},
        )
        assert out["eventType"] == AssetConstants.EventType.ALERT_GENERATED
        assert out["assetId"] == "conveyor-1"
        assert out["schemaVersion"] == "v1"
        assert "runId" not in out
        assert out["payload"]["severity"] == "error"
        assert out["payload"]["message"] == "Asset state: error"
        assert out["payload"]["metrics"][0]["metric"] == "power"
        assert out["payload"]["metrics"][0]["current"] == 260.0
        assert out["payload"]["metrics"][0]["threshold"] == 250
        assert out["payload"]["metrics"][0]["code"] == "POWER_OVERLOAD"

    def test_run_id_in_metadata_when_provided(self) -> None:
        ts = datetime.now(timezone.utc)
        out = build_alert_event(
            asset_id="freezer-1",
            timestamp=ts,
            status=AssetConstants.Status.WARNING,
            run_id="run-123",
        )
        assert out["schemaVersion"] == "v1"
        assert out["runId"] == "run-123"
        assert out["payload"]["metadata"] == {"runId": "run-123"}
        assert out["payload"]["severity"] == "warning"
        assert out["payload"]["message"] == "Asset state: warning"
