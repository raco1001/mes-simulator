"""Tests for recommendations API endpoints."""
from fastapi.testclient import TestClient

from main import app


class _FakeRepo:
    def __init__(self) -> None:
        self._items = {
            "r1": {
                "recommendationId": "r1",
                "status": "pending",
                "severity": "warning",
            }
        }

    def list_recommendations(self, status=None, severity=None):
        values = list(self._items.values())
        if status:
            values = [x for x in values if x.get("status") == status]
        if severity:
            values = [x for x in values if x.get("severity") == severity]
        return values

    def get_recommendation(self, recommendation_id):
        return self._items.get(recommendation_id)

    def update_recommendation_status(self, recommendation_id, status):
        item = self._items.get(recommendation_id)
        if item is None:
            return None
        item["status"] = status
        return item

    def close(self):
        return None


def test_recommendation_endpoints(monkeypatch) -> None:
    import api.recommendations as recommendations_module

    monkeypatch.setattr(recommendations_module, "AssetRepository", _FakeRepo)
    with TestClient(app) as client:
        list_resp = client.get("/recommendations")
        assert list_resp.status_code == 200
        assert len(list_resp.json()) == 1

        get_resp = client.get("/recommendations/r1")
        assert get_resp.status_code == 200
        assert get_resp.json()["recommendationId"] == "r1"

        patch_resp = client.patch("/recommendations/r1", json={"status": "approved"})
        assert patch_resp.status_code == 200
        assert patch_resp.json()["status"] == "approved"
