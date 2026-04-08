"""MongoDB object_type_schemas: ontology payload for pipeline."""
from typing import Any

from pymongo import MongoClient
from pymongo.collection import Collection
from pymongo.database import Database

from config.settings import Settings


class ObjectTypeRepository:
    """Read ObjectTypeSchema payloadJson from object_type_schemas collection."""

    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or Settings()
        self._client: MongoClient | None = None
        self._db: Database | None = None

    def _get_client(self) -> MongoClient:
        if self._client is None:
            self._client = MongoClient(self.settings.mongodb_url)
        return self._client

    def _get_database(self) -> Database:
        if self._db is None:
            self._db = self._get_client()[self.settings.mongodb_database]
        return self._db

    def get_by_object_type(self, object_type: str) -> dict[str, Any] | None:
        """Return payloadJson document (DTO fields including ownProperties), or None."""
        db = self._get_database()
        col: Collection = db["object_type_schemas"]
        doc = col.find_one({"objectType": object_type})
        if doc is None:
            return None
        payload = doc.get("payloadJson")
        if isinstance(payload, dict):
            return payload
        return None

    def close(self) -> None:
        if self._client:
            self._client.close()
            self._client = None
            self._db = None
