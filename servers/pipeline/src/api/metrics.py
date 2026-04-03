"""Metrics summary API routes."""
from fastapi import APIRouter

router = APIRouter(prefix="/metrics")


@router.get("/summary")
def metrics_summary() -> dict[str, int]:
    # Phase 14 MVP: endpoint contract first; values are wired in later iterations.
    return {
        "eventsProcessed": 0,
        "alertsGenerated": 0,
        "recommendationsGenerated": 0,
    }
