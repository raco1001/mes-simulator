"""Asset event and state DTOs."""
from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, Field


class AssetCreatedEventDto(BaseModel):
    """Asset created event DTO."""

    model_config = ConfigDict(populate_by_name=True)

    event_type: str = Field(..., alias="eventType")
    asset_id: str = Field(..., alias="assetId")
    timestamp: datetime
    payload: dict[str, Any]


class AssetHealthUpdatedEventDto(BaseModel):
    """Asset health updated event DTO."""

    model_config = ConfigDict(populate_by_name=True)

    event_type: str = Field(..., alias="eventType")
    asset_id: str = Field(..., alias="assetId")
    timestamp: datetime
    payload: dict[str, Any]


class SimulationStateUpdatedEventDto(BaseModel):
    """Simulation state updated event DTO (backend propagation)."""

    model_config = ConfigDict(populate_by_name=True)

    event_type: str = Field(..., alias="eventType")
    asset_id: str = Field(..., alias="assetId")
    timestamp: datetime
    payload: dict[str, Any]


class AssetStateDto(BaseModel):
    """Asset state DTO for MongoDB storage."""

    asset_id: str
    current_temp: float | None = None
    current_power: float | None = None
    status: str
    last_event_type: str | None = None
    updated_at: datetime
    metadata: dict[str, Any] = Field(default_factory=dict)
