"""Health domain constants (aligned with common health check semantics)."""


class HealthConstants:
    """Status names and defaults for health reporting."""

    class Status:
        Healthy = "healthy"
        Degraded = "degraded"
        Unhealthy = "unhealthy"

    class Defaults:
        ApplicationName = "pipeline"
