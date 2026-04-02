"""Tests for analysis pipeline utilities."""
from pipelines.analysis_pipeline import linear_trend, moving_average, time_to_threshold


def test_moving_average_handles_window() -> None:
    assert moving_average([1, 2, 3, 4, 5], window=3) == 4.0
    assert moving_average([], window=3) == 0.0


def test_linear_trend_returns_slope_and_intercept() -> None:
    slope, intercept = linear_trend([1, 2, 3], [2, 4, 6])
    assert round(slope, 4) == 2.0
    assert round(intercept, 4) == 0.0


def test_time_to_threshold() -> None:
    assert time_to_threshold(current=10, slope=2, threshold=20) == 5
    assert time_to_threshold(current=10, slope=0, threshold=20) is None
