Phase 9.4 Alert 토픽 분리 + Redis 제거 계획

Goal

alert.generated 이벤트를 factory.asset.alert 전용 토픽으로 분리하고, 코드에서 사용하지 않는 Redis/Redis Commander를 Compose에서 제거해 운영 구성을 단순화합니다.

Constraints

기존 end-to-end 동작(시뮬레이션 -> Alert 생성 -> Backend 소비 -> API/SSE)을 유지

토픽 계약(topics.json)과 런타임 코드(Pipeline/Backend) 정합성 유지

현재 규모(단일 인스턴스 포트폴리오)에서 불필요한 인프라 제거 우선

확장성은 코드 추가가 아닌 문서화(IAlertNotifier 구현체 교체 전략)로 남김

Acceptance Criteria

Pipeline이 alert.generated를 factory.asset.alert로 발행함

Backend KafkaAlertConsumerService가 TopicAlertEvents를 구독하고 eventType 필터 없이 처리함

topics.json에 factory.asset.alert가 active로 정의되고, factory.asset.events에서 alert.generated이 제거됨

docker-compose.infra.yml/docker-compose.full.yml에서 redis, redis-commander가 제거됨

관련 테스트(Pipeline/Backend)가 업데이트되어 통과함

아키텍처 문서에 Redis Pub/Sub 기반 SSE 확장 경로가 명시됨

Target Files

Pipeline

servers/pipeline/src/config/settings.py

servers/pipeline/src/workers/asset_worker.py

servers/pipeline/tests/workers/test_asset_worker.py

Backend

servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaOptions.cs

servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaAlertConsumerService.cs

servers/backend/DotnetEngine.Tests/Infrastructure/Kafka/KafkaAlertConsumerServiceTests.cs

계약/인프라

shared/event-schemas/topics/topics.json

docker/docker-compose.infra.yml

docker/docker-compose.full.yml

확장 근거 문서

documentation/backend/simulation-engine-architecture.md

Data Flow (after change)

flowchart TD
simEvents[Simulation events] --> assetTopic[factory.asset.events]
alertGen[alert.generated] --> alertTopic[factory.asset.alert]
alertTopic --> alertConsumer[KafkaAlertConsumerService]
alertConsumer --> alertStore[IAlertStore]
alertConsumer --> sseNotifier[IAlertNotifier]

Implementation Steps

Pipeline 토픽 분리

Settings에 kafka_topic_alert_events(default: factory.asset.alert) 추가

asset_worker.py의 alert publish 지점을 kafka_topic_asset_events -> kafka_topic_alert_events로 변경

테스트에서 alert publish topic assertion을 새 설정값 기준으로 수정/보강

Backend 토픽 분리

KafkaOptions에 TopicAlertEvents 추가

KafkaAlertConsumerService 구독 토픽을 TopicAlertEvents로 변경

전용 alert 토픽 가정에 맞춰 eventType == alert.generated 필터 로직 제거

테스트를 필터 제거 계약에 맞게 수정(유효 payload 처리 / 무효 payload 무시)

계약 파일 정합화

topics.json 스키마에 status(active|planned|deprecated) 반영

factory.asset.events에서 alert.generated 제거, factory.asset.alert 항목 추가 및 status: active 설정

Redis 제거

infra/full compose에서 redis, redis-commander, 관련 volume/depends_on/env 제거

문서 드리프트 최소화를 위해 필요 시 compose 관련 README의 Redis 언급도 정리

확장 경로 문서화

simulation-engine-architecture.md에 “Future: Redis Pub/Sub 기반 RedisBackedSseChannel로 IAlertNotifier 구현체 교체” 섹션 추가

검증

Pipeline 테스트 + Backend 테스트 실행

가능하면 빠른 smoke 확인(토픽 설정값/구독 경로)

Why this fits now

현재 시스템은 Alert 소비자가 일반 이벤트 토픽을 스캔하는 비효율이 있고, Redis는 코드에서 전혀 사용되지 않아 운영 복잡도만 증가시킵니다. 이번 변경은 토픽 경계를 명확히 하면서 불필요 인프라를 제거해 단순성을 높이고, 동시에 향후 다중 인스턴스 확장 시 IAlertNotifier 구현체만 교체하는 경로를 남겨 균형을 맞춥니다.
