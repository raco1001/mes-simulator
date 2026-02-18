#!/bin/bash

# Kafka 토픽 초기화 스크립트
# 사용법: ./init-topics.sh [kafka-container-name]
# 기본값: kafka-broker

KAFKA_CONTAINER=${1:-kafka-broker}
KAFKA_BOOTSTRAP="localhost:9092"

echo "Waiting for Kafka to be ready..."
until docker exec $KAFKA_CONTAINER kafka-broker-api-versions --bootstrap-server $KAFKA_BOOTSTRAP > /dev/null 2>&1; do
  echo "Waiting for Kafka..."
  sleep 2
done

echo "Kafka is ready. Creating topics..."

# factory.asset.events 토픽 생성
docker exec $KAFKA_CONTAINER kafka-topics \
  --create \
  --bootstrap-server $KAFKA_BOOTSTRAP \
  --topic factory.asset.events \
  --partitions 3 \
  --replication-factor 1 \
  --if-not-exists

# factory.asset.health 토픽 생성
docker exec $KAFKA_CONTAINER kafka-topics \
  --create \
  --bootstrap-server $KAFKA_BOOTSTRAP \
  --topic factory.asset.health \
  --partitions 3 \
  --replication-factor 1 \
  --if-not-exists

# factory.asset.alert 토픽 생성
docker exec $KAFKA_CONTAINER kafka-topics \
  --create \
  --bootstrap-server $KAFKA_BOOTSTRAP \
  --topic factory.asset.alert \
  --partitions 3 \
  --replication-factor 1 \
  --if-not-exists

echo "Topics created successfully!"
echo ""
echo "Listing all topics:"
docker exec $KAFKA_CONTAINER kafka-topics --list --bootstrap-server $KAFKA_BOOTSTRAP
