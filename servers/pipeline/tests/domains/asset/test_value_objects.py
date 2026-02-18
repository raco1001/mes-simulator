"""Tests for Asset domain value objects."""
from datetime import datetime, timezone

import pytest

from domains.asset import AssetConstants, AssetState, AssetStatus


class TestAssetStatus:
    """AssetStatus constants."""

    def test_status_constants(self) -> None:
        assert AssetStatus.NORMAL == AssetConstants.Status.NORMAL
        assert AssetStatus.WARNING == AssetConstants.Status.WARNING
        assert AssetStatus.ERROR == AssetConstants.Status.ERROR


class TestAssetState:
    """AssetState value object."""

    def test_required_fields(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
        )
        assert state.asset_id == "freezer-1"
        assert state.updated_at.tzinfo is not None

    def test_optional_fields(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            current_temp=-5.0,
            current_power=120.0,
            status=AssetConstants.Status.WARNING,
            last_event_type="asset.health.updated",
            updated_at=datetime.now(timezone.utc),
            metadata={"humidity": 45},
        )
        assert state.current_temp == -5.0
        assert state.current_power == 120.0
        assert state.status == AssetConstants.Status.WARNING
        assert state.last_event_type == "asset.health.updated"
        assert state.metadata["humidity"] == 45

    def test_default_status(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
        )
        assert state.status == AssetConstants.Status.NORMAL

    def test_default_metadata(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
        )
        assert state.metadata == {}

    def test_none_values(self) -> None:
        state = AssetState(
            asset_id="freezer-1",
            updated_at=datetime.now(timezone.utc),
            current_temp=None,
            current_power=None,
            last_event_type=None,
        )
        assert state.current_temp is None
        assert state.current_power is None
        assert state.last_event_type is None
