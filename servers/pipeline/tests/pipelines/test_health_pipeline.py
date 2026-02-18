"""Tests for get_health use case (health pipeline)."""
from unittest.mock import MagicMock

import pytest

from config.settings import Settings
from pipelines.health_pipeline import get_health
from pipelines.health_dto import HealthStatusDto


class TestGetHealth:
    """get_health() returns initialization state only (no infra)."""

    def test_returns_health_status_dto(self) -> None:
        result = get_health()
        assert isinstance(result, HealthStatusDto)

    def test_status_is_healthy_when_initialized(self) -> None:
        result = get_health()
        assert result.status == "healthy"

    def test_description_is_non_empty(self) -> None:
        result = get_health()
        assert isinstance(result.description, str)
        assert len(result.description) > 0

    def test_application_name_from_settings(self) -> None:
        settings = Settings(application_name="my-pipeline")
        result = get_health(settings=settings)
        assert result.application_name == "my-pipeline"

    def test_application_name_default_when_no_settings(self) -> None:
        result = get_health()
        assert result.application_name == "pipeline"

    def test_reported_at_is_utc_aware(self) -> None:
        result = get_health()
        assert result.reported_at.tzinfo is not None
