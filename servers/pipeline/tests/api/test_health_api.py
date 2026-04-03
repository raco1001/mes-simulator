"""Tests for health API endpoints."""
from fastapi.testclient import TestClient

from main import app


def test_health_endpoint() -> None:
    with TestClient(app) as client:
        resp = client.get("/health")
    assert resp.status_code == 200
    assert resp.json()["status"] == "healthy"


def test_ready_endpoint() -> None:
    with TestClient(app) as client:
        resp = client.get("/ready")
    assert resp.status_code == 200
    assert resp.json()["status"] == "ready"
