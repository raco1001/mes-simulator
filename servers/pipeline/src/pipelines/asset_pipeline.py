"""Asset pipeline: event processing and state calculation."""
from datetime import datetime, timezone
from typing import Any

from domains.asset import AssetConstants, AssetState
from pipelines.asset_dto import (
    AssetHealthUpdatedEventDto,
    AssetStateDto,
    SimulationStateUpdatedEventDto,
)


def calculate_state(
    event: AssetHealthUpdatedEventDto | SimulationStateUpdatedEventDto,
    asset_type: str = "unknown",
    schema: dict[str, Any] | None = None,
) -> AssetState:
    """
    Calculate asset state from health or simulation event.

    Rules:
    - If payload.status exists, use it.
    - Else if schema (ObjectType payloadJson) is provided, use ownProperties.alertThresholds.
    - Else evaluate payload.thresholds and legacy temperature/power defaults.
    """
    payload = event.payload
    properties = payload.get("properties")
    event_status = payload.get("status")

    if not isinstance(properties, dict):
        properties = {}

    thresholds = payload.get("thresholds", {})

    if event_status is not None and event_status != "":
        status = str(event_status)
    elif schema:
        status = _evaluate_alert_thresholds(properties, schema)
    else:
        status = _calculate_status_from_properties(properties, thresholds)

    # Extract metadata from envelope only (properties carries metrics)
    metadata = {
        k: v
        for k, v in payload.items()
        if k not in ["properties", "status", "deltaSeconds"]
    }
    _ = asset_type  # reserved for future logging / routing

    return AssetState(
        asset_id=event.asset_id,
        properties=properties,
        status=status,
        last_event_type=event.event_type,
        updated_at=event.timestamp if isinstance(event.timestamp, datetime) else datetime.now(timezone.utc),
        metadata=metadata,
    )


def _evaluate_alert_thresholds(properties: dict[str, Any], schema: dict[str, Any]) -> str:
    """
    Walk ownProperties[*].alertThresholds and return worst status:
    error > warning > normal.
    """
    severity_rank = {
        AssetConstants.Status.NORMAL: 0,
        AssetConstants.Status.WARNING: 1,
        AssetConstants.Status.ERROR: 2,
    }
    level_to_status = {
        "warning": AssetConstants.Status.WARNING,
        "error": AssetConstants.Status.ERROR,
    }
    worst = AssetConstants.Status.NORMAL

    for prop_def in schema.get("ownProperties", []):
        key = prop_def.get("key")
        thresholds = prop_def.get("alertThresholds") or []
        value = properties.get(key) if key is not None else None

        if value is None or not isinstance(value, (int, float)) or not thresholds:
            continue

        for t in thresholds:
            if not isinstance(t, dict):
                continue
            level = t.get("level")
            condition = t.get("condition")
            limit = t.get("value")
            if level not in level_to_status or condition not in ("lt", "lte", "gt", "gte"):
                continue
            if not isinstance(limit, (int, float)):
                continue

            breached = (
                (condition == "lt" and value < limit)
                or (condition == "lte" and value <= limit)
                or (condition == "gt" and value > limit)
                or (condition == "gte" and value >= limit)
            )
            if breached:
                candidate = level_to_status[level]
                if severity_rank.get(candidate, 0) > severity_rank.get(worst, 0):
                    worst = candidate

    return worst


def calculate_derived_properties(
    current_state: dict[str, Any],
    schema: dict[str, Any],
    delta_seconds: float,
) -> dict[str, Any]:
    """
    For simulationBehavior=Derived and derivedRule.type=linear, compute updated values.
    Does not mutate current_state.
    """
    time_unit_to_seconds = {"second": 1.0, "minute": 60.0, "hour": 3600.0}
    updates: dict[str, Any] = {}

    for prop_def in schema.get("ownProperties", []):
        if prop_def.get("simulationBehavior") != "Derived":
            continue

        rule = prop_def.get("derivedRule")
        if not isinstance(rule, dict) or rule.get("type") != "linear":
            continue

        key = prop_def.get("key")
        if not key:
            continue

        base = prop_def.get("baseValue", 0.0)
        current_val = current_state.get(key, base)
        if not isinstance(current_val, (int, float)):
            try:
                current_val = float(current_val)
            except (TypeError, ValueError):
                current_val = float(base) if isinstance(base, (int, float)) else 0.0

        time_unit = rule.get("timeUnit", "second")
        delta_units = delta_seconds / time_unit_to_seconds.get(str(time_unit), 1.0)

        delta = 0.0
        for inp in rule.get("inputs", []):
            if not isinstance(inp, dict):
                continue
            prop = inp.get("property")
            coef = inp.get("coefficient", 1.0)
            if not isinstance(coef, (int, float)):
                coef = 1.0
            ref = current_state.get(prop, 0.0)
            if isinstance(ref, (int, float)):
                delta += float(coef) * float(ref)

        delta *= delta_units
        new_val = float(current_val) + delta

        constraints = prop_def.get("constraints", {})
        if isinstance(constraints, dict):
            if "min" in constraints and isinstance(constraints["min"], (int, float)):
                new_val = max(new_val, float(constraints["min"]))
            if "max" in constraints and isinstance(constraints["max"], (int, float)):
                new_val = min(new_val, float(constraints["max"]))

        updates[str(key)] = round(new_val, 4)

    return updates


def _calculate_status_from_properties(
    properties: dict[str, Any],
    thresholds: Any,
) -> str:
    status = AssetConstants.Status.NORMAL
    rules = thresholds if isinstance(thresholds, dict) else {}
    for key, value in properties.items():
        if not isinstance(value, (int, float)):
            continue
        rule = rules.get(key) if isinstance(rules.get(key), dict) else {}
        error_threshold = rule.get("errorAbove")
        warning_threshold = rule.get("warningAbove")

        if isinstance(error_threshold, (int, float)) and value >= float(error_threshold):
            return AssetConstants.Status.ERROR
        if isinstance(warning_threshold, (int, float)) and value >= float(warning_threshold):
            status = AssetConstants.Status.WARNING

    # Backward-compatible defaults for widely used metrics.
    temperature = properties.get("temperature")
    if isinstance(temperature, (int, float)):
        if temperature >= 0:
            return AssetConstants.Status.ERROR
        if temperature >= -10:
            status = AssetConstants.Status.WARNING

    power = properties.get("power")
    if isinstance(power, (int, float)):
        if power > 250:
            return AssetConstants.Status.ERROR
        if power > 200:
            status = AssetConstants.Status.WARNING
    return status


def asset_state_to_dto(state: AssetState) -> AssetStateDto:
    """Convert AssetState to AssetStateDto for MongoDB storage."""
    return AssetStateDto(
        asset_id=state.asset_id,
        properties=state.properties,
        status=state.status,
        last_event_type=state.last_event_type,
        updated_at=state.updated_at,
        metadata=state.metadata,
    )


def build_alert_event(
    asset_id: str,
    timestamp: datetime,
    status: str,
    properties: dict[str, Any] | None = None,
    run_id: str | None = None,
) -> dict[str, Any]:
    """
    Build alert.generated event payload for Kafka.

    Call only when status is WARNING or ERROR.
    Maps status to severity and emits one or more breached metrics.
    """
    severity = "error" if status == AssetConstants.Status.ERROR else "warning"
    message = f"Asset state: {severity}"
    props = properties or {}
    metrics: list[dict[str, Any]] = []

    current_temp = props.get("temperature")
    if isinstance(current_temp, (int, float)):
        if status == AssetConstants.Status.ERROR and current_temp >= 0:
            metrics.append(
                {
                    "metric": "temperature",
                    "current": float(current_temp),
                    "threshold": 0,
                    "code": "TEMP_HIGH",
                    "severity": "error",
                }
            )
        elif status == AssetConstants.Status.WARNING and current_temp >= -10:
            metrics.append(
                {
                    "metric": "temperature",
                    "current": float(current_temp),
                    "threshold": -10,
                    "code": "TEMP_HIGH",
                    "severity": "warning",
                }
            )

    current_power = props.get("power")
    if isinstance(current_power, (int, float)):
        if status == AssetConstants.Status.ERROR and current_power > 250:
            metrics.append(
                {
                    "metric": "power",
                    "current": float(current_power),
                    "threshold": 250,
                    "code": "POWER_OVERLOAD",
                    "severity": "error",
                }
            )
        elif status == AssetConstants.Status.WARNING and current_power > 200:
            metrics.append(
                {
                    "metric": "power",
                    "current": float(current_power),
                    "threshold": 200,
                    "code": "POWER_OVERLOAD",
                    "severity": "warning",
                }
            )

    payload: dict[str, Any] = {
        "severity": severity,
        "message": message,
        "metrics": metrics,
    }
    if run_id is not None:
        payload["metadata"] = {"runId": run_id}

    ts_str = timestamp.isoformat() if isinstance(timestamp, datetime) else str(timestamp)
    out: dict[str, Any] = {
        "eventType": AssetConstants.EventType.ALERT_GENERATED,
        "assetId": asset_id,
        "timestamp": ts_str,
        "schemaVersion": "v1",
        "payload": payload,
    }
    if run_id is not None:
        out["runId"] = run_id
    return out
