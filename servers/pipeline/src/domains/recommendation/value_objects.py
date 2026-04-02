"""Recommendation domain value objects."""
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any
from uuid import uuid4


@dataclass(frozen=True)
class RecommendationOutcome:
    applied_at: datetime
    applied_run_id: str
    expected_change: dict[str, Any]
    actual_change: dict[str, Any] | None = None
    accuracy: float | None = None
    evaluated_at: datetime | None = None


@dataclass(frozen=True)
class Recommendation:
    id: str
    object_id: str
    object_type: str
    severity: str
    category: str
    title: str
    description: str
    suggested_action: dict[str, Any]
    analysis_basis: dict[str, Any]
    status: str = "pending"
    outcome: RecommendationOutcome | None = None
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    updated_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    @staticmethod
    def create(
        object_id: str,
        object_type: str,
        severity: str,
        category: str,
        title: str,
        description: str,
        suggested_action: dict[str, Any],
        analysis_basis: dict[str, Any],
    ) -> "Recommendation":
        now = datetime.now(timezone.utc)
        return Recommendation(
            id=str(uuid4()),
            object_id=object_id,
            object_type=object_type,
            severity=severity,
            category=category,
            title=title,
            description=description,
            suggested_action=suggested_action,
            analysis_basis=analysis_basis,
            status="pending",
            created_at=now,
            updated_at=now,
        )
