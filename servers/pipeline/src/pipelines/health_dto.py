"""Health check response DTO (application layer)."""
from datetime import datetime

from pydantic import BaseModel, Field


class HealthStatusDto(BaseModel):
    """Health status API/CLI response DTO."""

    status: str = Field(..., description="healthy | degraded | unhealthy")
    description: str = Field(..., description="Human-readable description")
    application_name: str = Field(..., description="Application identifier")
    reported_at: datetime = Field(..., description="When the check was performed")
