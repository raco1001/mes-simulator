"""MongoDB asset repository."""
from datetime import datetime
from typing import Any

from pymongo import MongoClient
from pymongo.collection import Collection
from pymongo.database import Database

from config.settings import Settings
from pipelines.asset_dto import AssetStateDto


class AssetRepository:
    """MongoDB repository for asset operations."""

    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or Settings()
        self._client: MongoClient | None = None
        self._db: Database | None = None

    def _get_client(self) -> MongoClient:
        """Get or create MongoDB client."""
        if self._client is None:
            self._client = MongoClient(self.settings.mongodb_url)
        return self._client

    def _get_database(self) -> Database:
        """Get database instance."""
        if self._db is None:
            client = self._get_client()
            self._db = client[self.settings.mongodb_database]
        return self._db

    def save_asset(
        self,
        asset_id: str,
        asset_type: str,
        connections: list[str] | None = None,
        metadata: dict[str, Any] | None = None,
    ) -> None:
        """Save or update asset metadata."""
        db = self._get_database()
        assets: Collection = db["assets"]

        now = datetime.now()
        assets.update_one(
            {"_id": asset_id},
            {
                "$set": {
                    "type": asset_type,
                    "connections": connections or [],
                    "metadata": metadata or {},
                    "updatedAt": now,
                },
                "$setOnInsert": {
                    "_id": asset_id,
                    "createdAt": now,
                },
            },
            upsert=True,
        )

    def save_event(
        self,
        asset_id: str,
        event_type: str,
        timestamp: datetime,
        payload: dict[str, Any],
    ) -> None:
        """Save raw event to events collection."""
        db = self._get_database()
        events: Collection = db["events"]

        events.insert_one(
            {
                "assetId": asset_id,
                "eventType": event_type,
                "timestamp": timestamp,
                "payload": payload,
            }
        )

    def save_state(self, state_dto: AssetStateDto) -> None:
        """Save or update asset state."""
        db = self._get_database()
        states: Collection = db["states"]

        state_dict = state_dto.model_dump()
        states.update_one(
            {"assetId": state_dto.asset_id},
            {"$set": state_dict},
            upsert=True,
        )

    def close(self) -> None:
        """Close MongoDB connection."""
        if self._client:
            self._client.close()
            self._client = None
            self._db = None
