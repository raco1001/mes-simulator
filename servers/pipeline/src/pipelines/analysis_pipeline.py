"""Analytics pipeline utilities."""
from typing import Sequence


def moving_average(values: Sequence[float], window: int = 5) -> float:
    if not values:
        return 0.0
    size = min(len(values), max(window, 1))
    return sum(values[-size:]) / size


def linear_trend(timestamps: Sequence[float], values: Sequence[float]) -> tuple[float, float]:
    if len(values) < 2 or len(timestamps) != len(values):
        return (0.0, values[-1] if values else 0.0)

    deltas_v = [values[i] - values[i - 1] for i in range(1, len(values))]
    deltas_t = [timestamps[i] - timestamps[i - 1] for i in range(1, len(timestamps))]
    avg_dt = sum(deltas_t) / len(deltas_t) if deltas_t else 0.0
    if avg_dt == 0:
        slope = 0.0
    else:
        slope = (sum(deltas_v) / len(deltas_v)) / avg_dt

    intercept = values[-1] - slope * timestamps[-1]
    return (slope, intercept)


def time_to_threshold(current: float, slope: float, threshold: float) -> float | None:
    if slope == 0:
        return None
    remaining = (threshold - current) / slope
    return remaining if remaining > 0 else None
