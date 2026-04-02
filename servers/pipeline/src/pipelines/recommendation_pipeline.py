"""Recommendation generation pipeline."""
from datetime import datetime, timezone
from typing import Any

from domains.recommendation.rules import (
    DepletionWarningRule,
    EfficiencyDropRule,
    OverheatWarningRule,
    RecommendationRule,
    TrendResult,
)
from domains.recommendation.value_objects import Recommendation
from pipelines.analysis_pipeline import linear_trend, moving_average, time_to_threshold


def build_trend_results(
    object_id: str,
    object_type: str,
    property_series: dict[str, list[tuple[float, float]]],
    thresholds: dict[str, float],
) -> list[TrendResult]:
    results: list[TrendResult] = []
    for key, series in property_series.items():
        if not series:
            continue
        timestamps = [t for t, _ in series]
        values = [v for _, v in series]
        slope, _ = linear_trend(timestamps, values)
        current = moving_average(values, window=3)
        threshold = thresholds.get(key, 0.0)
        eta = time_to_threshold(current=current, slope=slope, threshold=threshold)
        results.append(
            TrendResult(
                object_id=object_id,
                object_type=object_type,
                property_key=key,
                current=current,
                slope=slope,
                predicted_threshold_seconds=eta,
                threshold=threshold,
            )
        )
    return results


def generate_recommendations(
    trends: list[TrendResult],
    rules: list[RecommendationRule] | None = None,
) -> list[Recommendation]:
    active_rules = rules or [DepletionWarningRule(), OverheatWarningRule(), EfficiencyDropRule()]
    recommendations: list[Recommendation] = []
    for trend in trends:
        for rule in active_rules:
            rec = rule.evaluate(trend)
            if rec is not None:
                recommendations.append(rec)
    return recommendations


def recommendation_to_event_payload(recommendation: Recommendation) -> dict[str, Any]:
    return {
        "eventType": "recommendation.generated",
        "assetId": recommendation.object_id,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "schemaVersion": "v1",
        "payload": {
            "recommendationId": recommendation.id,
            "objectId": recommendation.object_id,
            "objectType": recommendation.object_type,
            "severity": recommendation.severity,
            "category": recommendation.category,
            "title": recommendation.title,
            "description": recommendation.description,
            "suggestedAction": recommendation.suggested_action,
            "analysisBasis": recommendation.analysis_basis,
            "status": recommendation.status,
            "createdAt": recommendation.created_at.isoformat(),
            "updatedAt": recommendation.updated_at.isoformat(),
        },
    }
