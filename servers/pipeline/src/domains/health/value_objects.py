"""Health domain value objects."""
from datetime import datetime

from domains.health.constants import HealthConstants


class HealthStatusKind:
    """Health status kind (aligned with common health check semantics)."""

    HEALTHY = "healthy"
    DEGRADED = "degraded"
    UNHEALTHY = "unhealthy"


class HealthReport:
    """Value object: result of a health check."""

    __slots__ = ("_status", "_description", "_application_name", "_reported_at")

    def __init__(
        self,
        status: str,
        description: str,
        application_name: str,
        reported_at: datetime,
    ) -> None:
        self._status = status or HealthConstants.Status.Unhealthy
        self._description = description or ""
        self._application_name = (
            (application_name or HealthConstants.Defaults.ApplicationName).strip()
            or HealthConstants.Defaults.ApplicationName
        )
        self._reported_at = reported_at

    @property
    def status(self) -> str:
        return self._status

    @property
    def description(self) -> str:
        return self._description

    @property
    def application_name(self) -> str:
        return self._application_name

    @property
    def reported_at(self) -> datetime:
        return self._reported_at
