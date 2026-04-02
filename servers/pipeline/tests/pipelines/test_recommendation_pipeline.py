"""Tests for recommendation pipeline."""
from pipelines.recommendation_pipeline import (
    build_trend_results,
    generate_recommendations,
    recommendation_to_event_payload,
)


def test_build_trend_results_creates_entries() -> None:
    trends = build_trend_results(
        object_id="battery-1",
        object_type="battery",
        property_series={"charge": [(1.0, 50.0), (2.0, 40.0)]},
        thresholds={"charge": 10.0},
    )
    assert len(trends) == 1
    assert trends[0].property_key == "charge"


def test_generate_recommendations_emits_matches() -> None:
    trends = build_trend_results(
        object_id="battery-1",
        object_type="battery",
        property_series={"charge": [(1.0, 50.0), (2.0, 40.0), (3.0, 20.0)]},
        thresholds={"charge": 10.0},
    )
    recs = generate_recommendations(trends)
    assert len(recs) >= 1


def test_recommendation_to_event_payload_shape() -> None:
    trends = build_trend_results(
        object_id="battery-1",
        object_type="battery",
        property_series={"charge": [(1.0, 50.0), (2.0, 20.0)]},
        thresholds={"charge": 10.0},
    )
    rec = generate_recommendations(trends)[0]
    evt = recommendation_to_event_payload(rec)
    assert evt["eventType"] == "recommendation.generated"
    assert evt["payload"]["recommendationId"] == rec.id
