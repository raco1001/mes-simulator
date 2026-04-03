"""Health and readiness API routes."""
from fastapi import APIRouter

from config.settings import Settings

router = APIRouter()


@router.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy"}


@router.get("/ready")
def ready() -> dict[str, str]:
    settings = Settings()
    return {
        "status": "ready",
        "kafkaBootstrapServers": settings.kafka_bootstrap_servers,
        "mongoDatabase": settings.mongodb_database,
    }
