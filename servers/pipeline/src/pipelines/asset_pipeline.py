"""Asset pipeline: event processing and state calculation."""
from datetime import datetime, timezone

from config.settings import Settings
from domains.asset import AssetConstants, AssetState, AssetStatus
from pipelines.asset_dto import AssetHealthUpdatedEventDto, AssetStateDto


def calculate_state(event: AssetHealthUpdatedEventDto) -> AssetState:
    """
    Calculate asset state from health updated event.

    Rules:
    - temperature >= -10: warning
    - temperature >= 0: error
    - power > 200: warning
    - power > 250: error
    - If event payload has status, use it; otherwise calculate from metrics
    """
    payload = event.payload
    temperature = payload.get("temperature")
    power = payload.get("power")
    event_status = payload.get("status")

    # Calculate status from metrics if not provided
    status = event_status or AssetConstants.Status.NORMAL

    if temperature is not None:
        if temperature >= 0:
            status = AssetConstants.Status.ERROR
        elif temperature >= -10:
            status = AssetConstants.Status.WARNING

    if power is not None:
        if power > 250:
            status = AssetConstants.Status.ERROR
        elif power > 200 and status != AssetConstants.Status.ERROR:
            status = AssetConstants.Status.WARNING

    # Extract metadata (excluding known fields)
    metadata = {
        k: v
        for k, v in payload.items()
        if k not in ["temperature", "power", "status"]
    }

    return AssetState(
        asset_id=event.asset_id,
        current_temp=temperature,
        current_power=power,
        status=status,
        last_event_type=event.event_type,
        updated_at=event.timestamp if isinstance(event.timestamp, datetime) else datetime.now(timezone.utc),
        metadata=metadata,
    )


def asset_state_to_dto(state: AssetState) -> AssetStateDto:
    """Convert AssetState to AssetStateDto for MongoDB storage."""
    return AssetStateDto(
        asset_id=state.asset_id,
        current_temp=state.current_temp,
        current_power=state.current_power,
        status=state.status,
        last_event_type=state.last_event_type,
        updated_at=state.updated_at,
        metadata=state.metadata,
    )
