"""Tests for Asset DTOs."""
from datetime import datetime, timezone

import pytest

from pipelines.asset_dto import (
    AssetCreatedEventDto,
    AssetHealthUpdatedEventDto,
    AssetStateDto,
)


class TestAssetCreatedEventDto:
    """AssetCreatedEventDto schema and serialization."""

    def test_required_fields(self) -> None:
        dto = AssetCreatedEventDto(
            eventType="asset.created",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"type": "freezer", "connections": []},
        )
        assert dto.event_type == "asset.created"
        assert dto.asset_id == "freezer-1"
        assert dto.timestamp.tzinfo is not None

    def test_alias_support(self) -> None:
        """Test that both snake_case and camelCase work."""
        dto = AssetCreatedEventDto(
            eventType="asset.created",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"type": "freezer"},
        )
        assert dto.event_type == "asset.created"
        assert dto.asset_id == "freezer-1"

    def test_payload_contains_metadata(self) -> None:
        payload = {
            "type": "freezer",
            "connections": ["conveyor-1"],
            "metadata": {"location": "floor-a"},
        }
        dto = AssetCreatedEventDto(
            eventType="asset.created",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload=payload,
        )
        assert dto.payload["type"] == "freezer"
        assert dto.payload["connections"] == ["conveyor-1"]
        assert dto.payload["metadata"]["location"] == "floor-a"


class TestAssetHealthUpdatedEventDto:
    """AssetHealthUpdatedEventDto schema and serialization."""

    def test_required_fields(self) -> None:
        dto = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={"temperature": -5, "power": 120},
        )
        assert dto.event_type == "asset.health.updated"
        assert dto.asset_id == "freezer-1"
        assert dto.payload["temperature"] == -5
        assert dto.payload["power"] == 120

    def test_optional_fields_in_payload(self) -> None:
        dto = AssetHealthUpdatedEventDto(
            eventType="asset.health.updated",
            assetId="freezer-1",
            timestamp=datetime.now(timezone.utc),
            payload={
                "temperature": -5,
                "power": 120,
                "status": "normal",
                "humidity": 45,
            },
        )
        assert dto.payload.get("humidity") == 45
        assert dto.payload.get("status") == "normal"


class TestAssetStateDto:
    """AssetStateDto schema and serialization."""

    def test_required_fields(self) -> None:
        dto = AssetStateDto(
            asset_id="freezer-1",
            status="normal",
            updated_at=datetime.now(timezone.utc),
        )
        assert dto.asset_id == "freezer-1"
        assert dto.status == "normal"
        assert dto.updated_at.tzinfo is not None

    def test_optional_fields(self) -> None:
        dto = AssetStateDto(
            asset_id="freezer-1",
            current_temp=-5.0,
            current_power=120.0,
            status="warning",
            last_event_type="asset.health.updated",
            updated_at=datetime.now(timezone.utc),
            metadata={"humidity": 45},
        )
        assert dto.current_temp == -5.0
        assert dto.current_power == 120.0
        assert dto.status == "warning"
        assert dto.last_event_type == "asset.health.updated"
        assert dto.metadata["humidity"] == 45

    def test_default_metadata(self) -> None:
        dto = AssetStateDto(
            asset_id="freezer-1",
            status="normal",
            updated_at=datetime.now(timezone.utc),
        )
        assert dto.metadata == {}

    def test_model_dump_json_serializable(self) -> None:
        now = datetime.now(timezone.utc)
        dto = AssetStateDto(
            asset_id="freezer-1",
            current_temp=-5.0,
            status="normal",
            updated_at=now,
        )
        payload = dto.model_dump(mode="json")
        assert payload["asset_id"] == "freezer-1"
        assert payload["current_temp"] == -5.0
        assert payload["status"] == "normal"
        assert isinstance(payload["updated_at"], str)  # ISO format string
