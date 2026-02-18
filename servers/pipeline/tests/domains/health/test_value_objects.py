"""Tests for health domain value objects (HealthReport, HealthStatusKind)."""
from datetime import datetime, timezone

import pytest

from domains.health.constants import HealthConstants
from domains.health.value_objects import HealthReport, HealthStatusKind


class TestHealthStatusKind:
    """Health status kind constants."""

    def test_constants_are_strings(self) -> None:
        assert HealthStatusKind.HEALTHY == "healthy"
        assert HealthStatusKind.DEGRADED == "degraded"
        assert HealthStatusKind.UNHEALTHY == "unhealthy"


class TestHealthReport:
    """HealthReport value object."""

    def test_construction_and_properties(self) -> None:
        now = datetime.now(timezone.utc)
        report = HealthReport(
            status="healthy",
            description="Ok",
            application_name="my-app",
            reported_at=now,
        )
        assert report.status == "healthy"
        assert report.description == "Ok"
        assert report.application_name == "my-app"
        assert report.reported_at == now

    def test_empty_application_name_defaults_to_constant(self) -> None:
        report = HealthReport(
            status="healthy",
            description="",
            application_name="   ",
            reported_at=datetime.now(timezone.utc),
        )
        assert report.application_name == HealthConstants.Defaults.ApplicationName

    def test_empty_status_treated_as_unhealthy(self) -> None:
        report = HealthReport(
            status="",
            description="",
            application_name="pipeline",
            reported_at=datetime.now(timezone.utc),
        )
        assert report.status == HealthConstants.Status.Unhealthy
