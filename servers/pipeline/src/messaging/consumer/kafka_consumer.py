"""Kafka consumer for asset events."""
import json
import logging
from datetime import datetime
from typing import Any

from kafka import KafkaConsumer
from kafka.errors import KafkaError

from config.settings import Settings

logger = logging.getLogger(__name__)


class AssetEventConsumer:
    """Kafka consumer for asset events."""

    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or Settings()
        self._consumer: KafkaConsumer | None = None

    def _get_consumer(self) -> KafkaConsumer:
        """Get or create Kafka consumer."""
        if self._consumer is None:
            self._consumer = KafkaConsumer(
                self.settings.kafka_topic_asset_events,
                bootstrap_servers=self.settings.kafka_bootstrap_servers.split(","),
                group_id=self.settings.kafka_consumer_group_id,
                value_deserializer=lambda m: json.loads(m.decode("utf-8")),
                auto_offset_reset="earliest",
                enable_auto_commit=True,
            )
        return self._consumer

    def consume(self) -> Any:
        """Consume messages from Kafka. Yields event dictionaries."""
        consumer = self._get_consumer()
        try:
            for message in consumer:
                try:
                    event = message.value
                    # Parse timestamp if it's a string
                    if isinstance(event.get("timestamp"), str):
                        event["timestamp"] = datetime.fromisoformat(
                            event["timestamp"].replace("Z", "+00:00")
                        )
                    yield event
                except Exception as e:
                    logger.error(f"Error processing message: {e}", exc_info=True)
        except KafkaError as e:
            logger.error(f"Kafka error: {e}", exc_info=True)
            raise

    def close(self) -> None:
        """Close Kafka consumer."""
        if self._consumer:
            self._consumer.close()
            self._consumer = None
