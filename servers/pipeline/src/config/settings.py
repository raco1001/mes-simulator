"""Application settings (pydantic-settings)."""
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Root settings. Env prefix for future: PIPELINE_."""

    model_config = SettingsConfigDict(
        env_prefix="PIPELINE_",
        env_file=".env",
        extra="ignore",
    )

    application_name: str = "pipeline"

    # Kafka settings
    kafka_bootstrap_servers: str = "localhost:9092"
    kafka_topic_asset_events: str = "factory.asset.events"
    kafka_consumer_group_id: str = "pipeline-asset-consumer"

    # MongoDB settings
    mongodb_url: str = "mongodb://admin:admin123@localhost:27017/factory_mes?authSource=admin"
    mongodb_database: str = "factory_mes"
