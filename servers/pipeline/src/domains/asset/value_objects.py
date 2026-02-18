"""Asset domain value objects."""
from datetime import datetime
from typing import Any

from domains.asset.constants import AssetConstants


class AssetStatus:
    """Asset status kind."""

    NORMAL = AssetConstants.Status.NORMAL
    WARNING = AssetConstants.Status.WARNING
    ERROR = AssetConstants.Status.ERROR


class AssetState:
    """Value object: Asset current state."""

    __slots__ = (
        "_asset_id",
        "_current_temp",
        "_current_power",
        "_status",
        "_last_event_type",
        "_updated_at",
        "_metadata",
    )

    def __init__(
        self,
        asset_id: str,
        updated_at: datetime,
        current_temp: float | None = None,
        current_power: float | None = None,
        status: str = AssetConstants.Status.NORMAL,
        last_event_type: str | None = None,
        metadata: dict[str, Any] | None = None,
    ) -> None:
        self._asset_id = asset_id
        self._current_temp = current_temp
        self._current_power = current_power
        self._status = status or AssetConstants.Status.NORMAL
        self._last_event_type = last_event_type
        self._updated_at = updated_at
        self._metadata = metadata or {}

    @property
    def asset_id(self) -> str:
        return self._asset_id

    @property
    def current_temp(self) -> float | None:
        return self._current_temp

    @property
    def current_power(self) -> float | None:
        return self._current_power

    @property
    def status(self) -> str:
        return self._status

    @property
    def last_event_type(self) -> str | None:
        return self._last_event_type

    @property
    def updated_at(self) -> datetime:
        return self._updated_at

    @property
    def metadata(self) -> dict[str, Any]:
        return self._metadata
