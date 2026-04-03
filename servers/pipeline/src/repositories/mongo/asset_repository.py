"""MongoDB asset repository. Writes state with camelCase keys per MongoDB convention."""
from datetime import datetime
from typing import Any

from pymongo import MongoClient
from pymongo.collection import Collection
from pymongo.database import Database

from config.settings import Settings
from pipelines.asset_dto import AssetStateDto


def _to_camel_case_key(key: str) -> str:
    """Convert snake_case to camelCase (e.g. asset_id -> assetId)."""
    if not key:
        return key
    parts = key.split("_")
    return parts[0].lower() + "".join(p.capitalize() for p in parts[1:])


def _dict_keys_to_camel(obj: Any) -> Any:
    """Recursively convert dict keys to camelCase for MongoDB."""
    if isinstance(obj, dict):
        return {_to_camel_case_key(k): _dict_keys_to_camel(v) for k, v in obj.items()}
    if isinstance(obj, list):
        return [_dict_keys_to_camel(i) for i in obj]
    return obj


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
        """Save or update asset state. Uses camelCase keys for MongoDB convention."""
        db = self._get_database()
        states: Collection = db["states"]

        # model_dump(by_alias=True) already yields camelCase (assetId, currentTemp, etc.)
        # Do NOT apply _dict_keys_to_camel: it would turn "assetId" into "assetid".
        state_dict = state_dto.model_dump(by_alias=True)
        states.update_one(
            {"assetId": state_dto.asset_id},
            {
                "$set": state_dict,
                "$setOnInsert": {"_id": state_dto.asset_id},
            },
            upsert=True,
        )

    def close(self) -> None:
        """Close MongoDB connection."""
        if self._client:
            self._client.close()
            self._client = None
            self._db = None

    def save_recommendation(self, recommendation: dict[str, Any]) -> None:
        """Insert recommendation document."""
        db = self._get_database()
        recommendations: Collection = db["recommendations"]
        recommendations.update_one(
            {"recommendationId": recommendation["recommendationId"]},
            {"$set": recommendation, "$setOnInsert": {"_id": recommendation["recommendationId"]}},
            upsert=True,
        )

    def list_recommendations(
        self,
        status: str | None = None,
        severity: str | None = None,
    ) -> list[dict[str, Any]]:
        db = self._get_database()
        recommendations: Collection = db["recommendations"]
        query: dict[str, Any] = {}
        if status:
            query["status"] = status
        if severity:
            query["severity"] = severity
        cursor = recommendations.find(query).sort("updatedAt", -1)
        docs = []
        for doc in cursor:
            doc.pop("_id", None)
            docs.append(doc)
        return docs

    def get_recommendation(self, recommendation_id: str) -> dict[str, Any] | None:
        db = self._get_database()
        recommendations: Collection = db["recommendations"]
        doc = recommendations.find_one({"recommendationId": recommendation_id})
        if doc is None:
            return None
        doc.pop("_id", None)
        return doc

    def update_recommendation_status(self, recommendation_id: str, status: str) -> dict[str, Any] | None:
        db = self._get_database()
        recommendations: Collection = db["recommendations"]
        recommendations.update_one(
            {"recommendationId": recommendation_id},
            {"$set": {"status": status, "updatedAt": datetime.now()}},
        )
        return self.get_recommendation(recommendation_id)

    def mark_recommendation_applied(
        self,
        recommendation_id: str,
        run_id: str,
        expected_change: dict[str, Any] | None = None,
    ) -> dict[str, Any] | None:
        db = self._get_database()
        recommendations: Collection = db["recommendations"]
        now = datetime.now()
        recommendations.update_one(
            {"recommendationId": recommendation_id},
            {
                "$set": {
                    "status": "applied",
                    "updatedAt": now,
                    "outcome": {
                        "appliedAt": now,
                        "appliedRunId": run_id,
                        "expectedChange": expected_change or {},
                        "actualChange": None,
                        "accuracy": None,
                        "evaluatedAt": None,
                    },
                }
            },
        )
        return self.get_recommendation(recommendation_id)

    def get_recent_property_series(
        self,
        asset_id: str,
        keys: list[str],
        limit: int = 20,
    ) -> dict[str, list[tuple[float, float]]]:
        db = self._get_database()
        events: Collection = db["events"]
        cursor = events.find({"assetId": asset_id, "payload.properties": {"$exists": True}}).sort("timestamp", -1).limit(limit)
        rows = list(cursor)
        rows.reverse()

        series = {k: [] for k in keys}
        for row in rows:
            payload = row.get("payload", {})
            props = payload.get("properties", {})
            timestamp = row.get("timestamp")
            if not isinstance(props, dict) or timestamp is None:
                continue
            t = timestamp.timestamp() if hasattr(timestamp, "timestamp") else 0.0
            for key in keys:
                value = props.get(key)
                if isinstance(value, (int, float)):
                    series[key].append((t, float(value)))
        return series
