"""Recommendation rules based on trend analysis."""
from dataclasses import dataclass
from typing import Protocol

from domains.recommendation.value_objects import Recommendation


@dataclass(frozen=True)
class TrendResult:
    object_id: str
    object_type: str
    property_key: str
    current: float
    slope: float
    predicted_threshold_seconds: float | None
    threshold: float


class RecommendationRule(Protocol):
    def evaluate(self, trend: TrendResult) -> Recommendation | None: ...


class DepletionWarningRule:
    def evaluate(self, trend: TrendResult) -> Recommendation | None:
        if trend.property_key not in {"charge", "level", "soc"}:
            return None
        if trend.slope >= 0:
            return None
        if trend.predicted_threshold_seconds is None or trend.predicted_threshold_seconds > 7200:
            return None
        return Recommendation.create(
            object_id=trend.object_id,
            object_type=trend.object_type,
            severity="warning",
            category="efficiency",
            title="Potential depletion detected",
            description="Current trend indicates depletion in less than 2 hours.",
            suggested_action={"property": trend.property_key, "targetValue": trend.current + 10},
            analysis_basis={
                "trendSlope": trend.slope,
                "predictedThresholdSeconds": trend.predicted_threshold_seconds,
            },
        )


class OverheatWarningRule:
    def evaluate(self, trend: TrendResult) -> Recommendation | None:
        if trend.property_key not in {"temperature", "temp"}:
            return None
        if trend.slope <= 0:
            return None
        if trend.predicted_threshold_seconds is None or trend.predicted_threshold_seconds > 1800:
            return None
        return Recommendation.create(
            object_id=trend.object_id,
            object_type=trend.object_type,
            severity="critical",
            category="safety",
            title="Overheat risk detected",
            description="Temperature trend indicates threshold breach in less than 30 minutes.",
            suggested_action={"property": trend.property_key, "targetValue": trend.threshold - 5},
            analysis_basis={
                "trendSlope": trend.slope,
                "predictedThresholdSeconds": trend.predicted_threshold_seconds,
            },
        )


class EfficiencyDropRule:
    def evaluate(self, trend: TrendResult) -> Recommendation | None:
        if trend.property_key not in {"efficiency", "throughput"}:
            return None
        if trend.slope >= 0:
            return None
        return Recommendation.create(
            object_id=trend.object_id,
            object_type=trend.object_type,
            severity="info",
            category="maintenance",
            title="Efficiency drop detected",
            description="Trend indicates sustained efficiency decline.",
            suggested_action={"property": trend.property_key, "targetValue": trend.current * 1.1},
            analysis_basis={"trendSlope": trend.slope},
        )
