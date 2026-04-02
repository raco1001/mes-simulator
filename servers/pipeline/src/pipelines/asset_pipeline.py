"""Asset pipeline: event processing and state calculation."""
from datetime import datetime, timezone
from typing import Any

from config.settings import Settings
from domains.asset import AssetConstants, AssetState, AssetStatus
from pipelines.asset_dto import AssetHealthUpdatedEventDto, AssetStateDto


def calculate_state(event: AssetHealthUpdatedEventDto) -> AssetState:
    """
    Calculate asset state from health updated event.

    Rules:
    - If payload.status exists, use it.
    - Else evaluate numeric properties against threshold metadata when provided.
    - If no threshold metadata is provided, fallback to legacy defaults for temperature/power only.
    """
    payload = event.payload
    properties = payload.get("properties")
    event_status = payload.get("status")

    if not isinstance(properties, dict):
        properties = {}

    thresholds = payload.get("thresholds", {})
    status = event_status or AssetConstants.Status.NORMAL
    if event_status is None:
        status = _calculate_status_from_properties(properties, thresholds)

    # Extract metadata from envelope only (properties carries metrics)
    metadata = {
        k: v
        for k, v in payload.items()
        if k not in ["properties", "status"]
    }

    return AssetState(
        asset_id=event.asset_id,
        properties=properties,
        status=status,
        last_event_type=event.event_type,
        updated_at=event.timestamp if isinstance(event.timestamp, datetime) else datetime.now(timezone.utc),
        metadata=metadata,
    )


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
