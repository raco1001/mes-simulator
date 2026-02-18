"""Tests for AssetRepository."""
from datetime import datetime, timezone
from unittest.mock import MagicMock, patch

import pytest

from config.settings import Settings
from pipelines.asset_dto import AssetStateDto
from repositories.mongo.asset_repository import AssetRepository


class TestAssetRepository:
    """AssetRepository MongoDB operations."""

    @pytest.fixture
    def mock_mongo_client(self) -> MagicMock:
        """Mock MongoDB client."""
        with patch("repositories.mongo.asset_repository.MongoClient") as mock_client:
            mock_db = MagicMock()
            mock_client.return_value.__getitem__.return_value = mock_db
            mock_client.return_value.__enter__ = MagicMock(return_value=mock_db)
            mock_client.return_value.__exit__ = MagicMock(return_value=False)
            yield mock_client

    @pytest.fixture
    def repository(self, mock_mongo_client: MagicMock) -> AssetRepository:
        """Create repository instance with mocked MongoDB."""
        repo = AssetRepository()
        repo._client = mock_mongo_client.return_value
        repo._db = mock_mongo_client.return_value["factory_mes"]
        return repo

    def test_save_asset_creates_or_updates(self, repository: AssetRepository) -> None:
        """Test saving asset metadata."""
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection

        repository.save_asset(
            asset_id="freezer-1",
            asset_type="freezer",
            connections=["conveyor-1"],
            metadata={"location": "floor-a"},
        )

        mock_collection.update_one.assert_called_once()
        call_args = mock_collection.update_one.call_args
        assert call_args[0][0] == {"_id": "freezer-1"}
        assert "$set" in call_args[0][1]
        assert "$setOnInsert" in call_args[0][1]
        assert call_args[1]["upsert"] is True

    def test_save_event_inserts_document(self, repository: AssetRepository) -> None:
        """Test saving raw event."""
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection

        timestamp = datetime.now(timezone.utc)
        repository.save_event(
            asset_id="freezer-1",
            event_type="asset.health.updated",
            timestamp=timestamp,
            payload={"temperature": -5},
        )

        mock_collection.insert_one.assert_called_once()
        call_args = mock_collection.insert_one.call_args[0][0]
        assert call_args["assetId"] == "freezer-1"
        assert call_args["eventType"] == "asset.health.updated"
        assert call_args["timestamp"] == timestamp
        assert call_args["payload"] == {"temperature": -5}

    def test_save_state_upserts_document(self, repository: AssetRepository) -> None:
        """Test saving asset state."""
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection

        state_dto = AssetStateDto(
            asset_id="freezer-1",
            current_temp=-5.0,
            current_power=120.0,
            status="normal",
            last_event_type="asset.health.updated",
            updated_at=datetime.now(timezone.utc),
            metadata={},
        )

        repository.save_state(state_dto)

        mock_collection.update_one.assert_called_once()
        call_args = mock_collection.update_one.call_args
        assert call_args[0][0] == {"assetId": "freezer-1"}
        assert "$set" in call_args[0][1]
        assert call_args[1]["upsert"] is True

    def test_close_closes_client(self, repository: AssetRepository) -> None:
        """Test closing repository connection."""
        # Ensure client is initialized
        mock_client = repository._client
        repository.close()
        mock_client.close.assert_called_once()
        assert repository._client is None
        assert repository._db is None
