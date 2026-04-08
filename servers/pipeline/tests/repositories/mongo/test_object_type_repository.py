"""Tests for ObjectTypeRepository."""
from unittest.mock import MagicMock, patch

import pytest

from repositories.mongo.object_type_repository import ObjectTypeRepository


class TestObjectTypeRepository:
    @pytest.fixture
    def mock_mongo_client(self) -> MagicMock:
        with patch("repositories.mongo.object_type_repository.MongoClient") as mock_client:
            mock_db = MagicMock()
            mock_client.return_value.__getitem__.return_value = mock_db
            yield mock_client

    @pytest.fixture
    def repository(self, mock_mongo_client: MagicMock) -> ObjectTypeRepository:
        repo = ObjectTypeRepository()
        repo._client = mock_mongo_client.return_value
        repo._db = mock_mongo_client.return_value["factory_mes"]
        return repo

    def test_get_by_object_type_returns_payload_json(self, repository: ObjectTypeRepository) -> None:
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection
        payload = {"objectType": "Drone", "ownProperties": []}
        mock_collection.find_one.return_value = {"objectType": "Drone", "payloadJson": payload}

        assert repository.get_by_object_type("Drone") == payload
        mock_collection.find_one.assert_called_once_with({"objectType": "Drone"})

    def test_get_by_object_type_returns_none_when_missing(self, repository: ObjectTypeRepository) -> None:
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection
        mock_collection.find_one.return_value = None
        assert repository.get_by_object_type("X") is None

    def test_get_by_object_type_returns_none_when_payload_not_dict(
        self, repository: ObjectTypeRepository
    ) -> None:
        mock_collection = MagicMock()
        repository._db.__getitem__.return_value = mock_collection
        mock_collection.find_one.return_value = {"objectType": "Drone", "payloadJson": "bad"}

        assert repository.get_by_object_type("Drone") is None
