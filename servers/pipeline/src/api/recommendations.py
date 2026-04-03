"""Recommendation API routes."""
from typing import Any

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from repositories.mongo.asset_repository import AssetRepository

router = APIRouter(prefix="/recommendations")


class RecommendationStatusPatch(BaseModel):
    status: str


@router.get("")
def list_recommendations(status: str | None = None, severity: str | None = None) -> list[dict[str, Any]]:
    repo = AssetRepository()
    try:
        return repo.list_recommendations(status=status, severity=severity)
    finally:
        repo.close()


@router.get("/{recommendation_id}")
def get_recommendation(recommendation_id: str) -> dict[str, Any]:
    repo = AssetRepository()
    try:
        rec = repo.get_recommendation(recommendation_id)
    finally:
        repo.close()
    if rec is None:
        raise HTTPException(status_code=404, detail="Recommendation not found")
    return rec


@router.patch("/{recommendation_id}")
def patch_recommendation(recommendation_id: str, patch: RecommendationStatusPatch) -> dict[str, Any]:
    repo = AssetRepository()
    try:
        updated = repo.update_recommendation_status(recommendation_id, patch.status)
    finally:
        repo.close()
    if updated is None:
        raise HTTPException(status_code=404, detail="Recommendation not found")
    return updated
