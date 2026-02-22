"""Kafka producer for asset/alert events."""
import json
import logging
from datetime import datetime
from typing import Any

from kafka import KafkaProducer
from kafka.errors import KafkaError

from config.settings import Settings

logger = logging.getLogger(__name__)


def _json_serializer(value: Any) -> bytes:
    """Serialize value to JSON bytes; datetime to ISO format."""
    def default(obj: Any) -> Any:
        if isinstance(obj, datetime):
            return obj.isoformat()
        raise TypeError(f"Object of type {type(obj).__name__} is not JSON serializable")

    return json.dumps(value, default=default).encode("utf-8")


class AssetEventProducer:
    """Kafka producer for asset events (e.g. alert.generated)."""

    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or Settings()
        self._producer: KafkaProducer | None = None

    def _get_producer(self) -> KafkaProducer:
        """Get or create Kafka producer."""
        if self._producer is None:
            self._producer = KafkaProducer(
                bootstrap_servers=self.settings.kafka_bootstrap_servers.split(","),
                value_serializer=_json_serializer,
            )
        return self._producer

    def send(self, topic: str, value: dict[str, Any]) -> None:
        """Send event dict to topic (JSON-serialized)."""
        try:
            producer = self._get_producer()
            producer.send(topic, value=value)
            producer.flush()
        except KafkaError as e:
            logger.error(f"Kafka send error: {e}", exc_info=True)
            raise

    def close(self) -> None:
        """Close Kafka producer."""
        if self._producer:
            self._producer.close()
            self._producer = None
