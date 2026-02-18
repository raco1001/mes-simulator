"""Tests for HealthStatusDto (application layer DTO)."""
from datetime import datetime, timezone

import pytest

from pipelines.health_dto import HealthStatusDto


class TestHealthStatusDto:
    """HealthStatusDto schema and serialization."""

    def test_required_fields(self) -> None:
        dto = HealthStatusDto(
            status="healthy",
            description="Running",
            application_name="pipeline",
            reported_at=datetime.now(timezone.utc),
        )
        assert dto.status == "healthy"
        assert dto.description == "Running"
        assert dto.application_name == "pipeline"
        assert dto.reported_at.tzinfo is not None

    def test_model_dump_json_serializable(self) -> None:
        now = datetime.now(timezone.utc)
        dto = HealthStatusDto(
            status="healthy",
            description="Ok",
            application_name="app",
            reported_at=now,
        )
        payload = dto.model_dump(mode="json")
        assert payload["status"] == "healthy"
        assert "reported_at" in payload
        # ISO format string when mode="json"
        assert isinstance(payload["reported_at"], str)
