Phase 8.4 topics.json 현실 반영

Goal

shared/event-schemas/topics/topics.json을 실제 코드가 사용하는 Kafka 토픽 구조와 일치시킵니다.

현재 운영 모델(단일 통합 토픽 factory.asset.events)을 계약 문서에 명확히 고정합니다.

Constraints

기존 런타임 동작 변경 없이 문서/계약 정합성에 집중합니다.

백엔드/파이프라인 코드가 이미 단일 토픽을 사용하므로, 다중 토픽 모델로 확장하지 않습니다.

기존 이벤트 스키마/버전 매니페스트와 모순되지 않게 최소 수정합니다.

Acceptance Criteria

[shared/event-schemas/topics/topics.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/topics/topics.json)에서 실사용 단일 토픽만 유지

단일 토픽 factory.asset.events의 이벤트 목록이 최신 public 이벤트와 일치 (asset.created, asset.health.updated, simulation.state.updated, alert.generated)

제거된 토픽(factory.asset.health, factory.asset.alert)은 문서에서 deprecated/미사용으로 정리

[shared/event-schemas/README.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/README.md)의 토픽 설명이 실제 구조와 일치

Implementation Steps

실사용 토픽 계약 반영

파일: [shared/event-schemas/topics/topics.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/topics/topics.json)

변경:

topics 배열에서 factory.asset.events만 유지

events에 simulation.state.updated 추가

description을 단일 통합 토픽 전략에 맞게 정리

스키마-이벤트 정합성 점검

파일:

[shared/event-schemas/versions/v1.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/versions/v1.json)

[shared/event-schemas/schemas/\*.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas)

작업:

topics 이벤트 목록이 manifest 등록 이벤트와 일치하는지 확인

문서 동기화

파일: [shared/event-schemas/README.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/README.md)

변경:

Kafka 토픽 섹션을 단일 토픽 기준으로 수정

과거 분리 토픽(health, alert)은 미사용/legacy로 짧게 명시(필요 시)

구현 근거 캡처(코드 참조)

백엔드 실사용: [servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaOptions.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaOptions.cs), [KafkaEventPublisher.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaEventPublisher.cs), [KafkaAlertConsumerService.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaAlertConsumerService.cs)

파이프라인 실사용: [servers/pipeline/src/config/settings.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/src/config/settings.py), [servers/pipeline/src/messaging/consumer/kafka_consumer.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/src/messaging/consumer/kafka_consumer.py), [servers/pipeline/src/workers/asset_worker.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/src/workers/asset_worker.py)

Why this fits now

코드가 이미 단일 토픽 모델로 수렴되어 있어 topics.json만 정렬해도 즉시 drift가 해소됩니다.

이후 멀티 토픽으로 확장하더라도, 현재 기준선을 명확히 해두면 Phase 10~11 검증/호환성 관리가 쉬워집니다.
