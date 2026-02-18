"""Health check use case: returns current application health (no infra checks)."""
from datetime import datetime, timezone

from config.settings import Settings
from domains.health import HealthReport, HealthStatusKind
from pipelines.health_dto import HealthStatusDto


def get_health(settings: Settings | None = None) -> HealthStatusDto:
    """
    Return current health status (initialization state only; no Kafka/Redis/etc).
    """
    settings = settings or Settings()
    report = HealthReport(
        status=HealthStatusKind.HEALTHY,
        description="Application is running.",
        application_name=settings.application_name,
        reported_at=datetime.now(timezone.utc),
    )
    return HealthStatusDto(
        status=report.status,
        description=report.description,
        application_name=report.application_name,
        reported_at=report.reported_at,
    )
