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
