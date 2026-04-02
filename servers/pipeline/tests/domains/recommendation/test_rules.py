"""Tests for recommendation rules."""
from domains.recommendation.rules import (
    DepletionWarningRule,
    EfficiencyDropRule,
    OverheatWarningRule,
    TrendResult,
)


def test_depletion_warning_rule_matches() -> None:
    trend = TrendResult(
        object_id="battery-1",
        object_type="battery",
        property_key="charge",
        current=20,
        slope=-0.01,
        predicted_threshold_seconds=1000,
        threshold=10,
    )
    rec = DepletionWarningRule().evaluate(trend)
    assert rec is not None


def test_overheat_warning_rule_matches() -> None:
    trend = TrendResult(
        object_id="freezer-1",
        object_type="freezer",
        property_key="temperature",
        current=-2,
        slope=0.2,
        predicted_threshold_seconds=100,
        threshold=0,
    )
    rec = OverheatWarningRule().evaluate(trend)
    assert rec is not None


def test_efficiency_drop_rule_matches() -> None:
    trend = TrendResult(
        object_id="line-1",
        object_type="conveyor",
        property_key="efficiency",
        current=70,
        slope=-0.5,
        predicted_threshold_seconds=None,
        threshold=80,
    )
    rec = EfficiencyDropRule().evaluate(trend)
    assert rec is not None
