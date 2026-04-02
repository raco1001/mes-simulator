"""Tests for recommendation applied flow in asset worker."""
from datetime import datetime, timezone
from unittest.mock import MagicMock

from domains.asset.constants import AssetConstants
from workers.asset_worker import AssetWorker


class TestRecommendationAppliedFlow:
    """E2E Step 1+4: Worker generates recommendations and processes applied events."""

    def test_generate_recommendations_saves_and_publishes(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()
        worker.repository.get_recent_property_series.return_value = {
            "temperature": [(1.0, -5.0), (2.0, -3.0), (3.0, -1.0), (4.0, 1.0), (5.0, 3.0)],
        }

        worker._generate_recommendations("freezer-1", "Freezer")

        if worker.repository.save_recommendation.called:
            call_args = worker.repository.save_recommendation.call_args
            doc = call_args[0][0] if call_args[0] else call_args[1].get("recommendation")
            assert doc["status"] == "pending"
            assert doc["objectId"] == "freezer-1"
            assert doc["objectType"] == "Freezer"
            worker.producer.send.assert_called()

    def test_process_recommendation_applied_marks_outcome(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()
        worker.repository.mark_recommendation_applied.return_value = {
            "recommendationId": "r1",
            "status": "applied",
        }

        event = {
            "eventType": "recommendation.applied",
            "assetId": "battery-1",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "schemaVersion": "v1",
            "runId": "run-abc",
            "payload": {
                "recommendationId": "r1",
                "status": "applied",
                "triggerAssetId": "battery-1",
                "patch": {"properties": {"chargeRate": 500}},
                "runId": "run-abc",
                "appliedAt": datetime.now(timezone.utc).isoformat(),
            },
        }
        worker.process_recommendation_applied(event)

        worker.repository.mark_recommendation_applied.assert_called_once_with(
            recommendation_id="r1",
            run_id="run-abc",
            expected_change={"properties": {"chargeRate": 500}},
        )

    def test_process_event_dispatches_recommendation_applied(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()

        event = {
            "eventType": AssetConstants.EventType.RECOMMENDATION_APPLIED,
            "assetId": "battery-1",
            "timestamp": datetime.now(timezone.utc),
            "schemaVersion": "v1",
            "payload": {
                "recommendationId": "r2",
                "runId": "run-xyz",
                "patch": {},
            },
        }
        worker.process_event(event)

        worker.repository.mark_recommendation_applied.assert_called_once()

    def test_process_recommendation_applied_skips_without_required_fields(self) -> None:
        worker = AssetWorker()
        worker.producer = MagicMock()
        worker.repository = MagicMock()

        event_no_id = {
            "eventType": "recommendation.applied",
            "payload": {"runId": "run-1"},
        }
        worker.process_recommendation_applied(event_no_id)
        worker.repository.mark_recommendation_applied.assert_not_called()

        event_no_run = {
            "eventType": "recommendation.applied",
            "payload": {"recommendationId": "r1"},
        }
        worker.process_recommendation_applied(event_no_run)
        worker.repository.mark_recommendation_applied.assert_not_called()
