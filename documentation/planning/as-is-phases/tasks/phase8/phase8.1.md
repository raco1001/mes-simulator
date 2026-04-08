Phase 8.1 이벤트 엔벨로프 정합화

Goal

shared/를 SSoT로 만들기 위해 공통 엔벨로프 스키마를 도입하고, 이벤트 스키마/발행 코드가 동일 계약(eventType, assetId, timestamp, schemaVersion, payload, optional runId)을 따르도록 정렬합니다.

Constraints

기존 동작 깨짐 최소화: 기존 payload.metadata.runId를 유지하면서 top-level runId를 병행합니다.

현재 스택 유지: JSON Schema draft-07, 기존 .NET/Python 코드 구조를 크게 바꾸지 않습니다.

문서 범위 준수: Phase 8.1 산출물 중심(event-envelope.json, 3개 이벤트 스키마 ref 구조, KafkaEventPublisher.cs, asset_pipeline.py).

Acceptance Criteria

[shared/event-schemas/schemas/event-envelope.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/event-envelope.json) 신규 생성

asset.created / asset.health.updated / alert.generated가 공통 엔벨로프를 $ref(allOf)로 참조

.NET Kafka 발행 메시지에 schemaVersion: "v1" 포함

Python build_alert_event 반환 메시지에 schemaVersion: "v1" 및 top-level runId 포함

Pipeline 테스트가 새 계약을 검증하고 통과

[ 추가 ] event-envelope.json의 schemaVersion이 const "v1" 대신 pattern "^v\\d+$"로 정의됨

[ 추가 ] event-envelope.json에 optional context 객체(additionalProperties: true)가 포함됨

Implementation Plan

공통 엔벨로프 스키마 추가

신규 파일: [shared/event-schemas/schemas/event-envelope.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/event-envelope.json)

핵심 규칙:

required: eventType, assetId, timestamp, schemaVersion, payload

optional: runId

schemaVersion은 문자열(v1 현재값)

additionalProperties: false로 top-level 필드 제어

기존 이벤트 스키마 3종을 envelope + payload 확장 구조로 재작성

대상:

[shared/event-schemas/schemas/asset.created.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.created.json)

[shared/event-schemas/schemas/asset.health.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.health.updated.json)

[shared/event-schemas/schemas/alert.generated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/alert.generated.json)

방식:

allOf로 event-envelope.json 참조

각 이벤트별 제약은 properties.eventType.const + properties.payload(이벤트별 payload 스키마)로 덮어쓰기

예시(examples)도 schemaVersion/optional runId 반영

.NET 발행 객체에 schemaVersion 추가

대상: [servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaEventPublisher.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaEventPublisher.cs)

변경:

메시지 객체에 schemaVersion = "v1" 추가

기존 runId 매핑 유지

Pipeline alert event 생성 계약 업데이트

대상: [servers/pipeline/src/pipelines/asset_pipeline.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/src/pipelines/asset_pipeline.py)

변경:

반환 객체 top-level에 schemaVersion: "v1" 추가

top-level runId 추가(있을 때)

호환성 위해 기존 payload.metadata.runId 유지

테스트 보강 및 검증

우선 대상: [servers/pipeline/tests/pipelines/test_asset_pipeline.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/tests/pipelines/test_asset_pipeline.py), [servers/pipeline/tests/workers/test_asset_worker.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/tests/workers/test_asset_worker.py)

검증 포인트:

schemaVersion == "v1"

runId top-level + payload.metadata.runId 동시 확인

실행:

pipeline pytest 관련 스위트 실행

필요 시 backend 단위 테스트 영향 범위 확인(발행 JSON 필드 assertions 존재 여부 기준)

Dynamic Field 설계 보강 (추가 항목)

1. schemaVersion: const → pattern 완화

배경: const: "v1"은 v1 스키마가 v1 메시지만 통과시킨다. Phase 10.1의 런타임 검증 도입 시 소비자가 schemaVersion을 먼저 읽고 적절한 스키마 파일로 라우팅하는 구조가 필요한데, 라우팅 전에 엔벨로프 검증 자체가 실패하면 DLQ 폭발로 이어진다.

변경:

```json
"schemaVersion": {
  "type": "string",
  "pattern": "^v\\d+$",
  "description": "엔벨로프 계약 버전. 소비자는 이 값으로 페이로드 스키마를 라우팅한다."
}
```

효과: 어떤 버전의 메시지든 엔벨로프 검증 통과 → 소비자 코드에서 버전별 처리 분기 → Phase 11.2 호환성 체크와 자연 연결.

2. context 확장 포인트 추가

배경: additionalProperties: false 엔벨로프에서 traceId, correlationId, sourceService 같은 운영 메타데이터를 추가하려면 매번 버전 bump가 필요하다. 거버넌스 프로젝트는 이런 운영 필드를 "공식 계약 외 영역"으로 분리해서 처리해야 한다.

변경: event-envelope.json에 optional context 필드 추가:

```json
"context": {
  "type": "object",
  "description": "운영 메타데이터 (traceId, correlationId, sourceService 등). 공식 페이로드 계약과 분리.",
  "additionalProperties": true
}
```

효과: 엔벨로프의 공식 필드(eventType, assetId 등)는 여전히 additionalProperties: false로 엄격하게 제어. 운영/추적 목적 동적 필드는 context 안으로 수렴. 거버넌스 프로젝트가 context를 별도 레이어로 관리 가능.

Why this fits now / later scale

현재는 단일 엔벨로프 스키마 + 코드 필드 추가만으로 drift를 크게 줄이며 복잡도 증가가 작습니다(clarity 우선).

schemaVersion pattern + context 확장 포인트는 Phase 8.1 단계에서 추가 비용이 거의 없지만, 이후 Phase 10.1 런타임 검증과 Phase 11.2 호환성 체크에서 반드시 필요한 기반입니다.

이후 규모 확대 시에는 schemaVersion 기반 다중 버전 공존, CI 호환성 체크(Phase 11.2), 런타임 검증(Phase 10.x)로 자연 확장 가능합니다.

Phase 8.1 이벤트 엔벨로프 정합화

Goal

shared/를 SSoT로 만들기 위해 공통 엔벨로프 스키마를 도입하고, 이벤트 스키마/발행 코드가 동일 계약(eventType, assetId, timestamp, schemaVersion, payload, optional runId)을 따르도록 정렬합니다.

Constraints

기존 동작 깨짐 최소화: 기존 payload.metadata.runId를 유지하면서 top-level runId를 병행합니다.

현재 스택 유지: JSON Schema draft-07, 기존 .NET/Python 코드 구조를 크게 바꾸지 않습니다.

문서 범위 준수: Phase 8.1 산출물 중심(event-envelope.json, 3개 이벤트 스키마 ref 구조, KafkaEventPublisher.cs, asset_pipeline.py).

Acceptance Criteria

[shared/event-schemas/schemas/event-envelope.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/event-envelope.json) 신규 생성

asset.created / asset.health.updated / alert.generated가 공통 엔벨로프를 $ref(allOf)로 참조

.NET Kafka 발행 메시지에 schemaVersion: "v1" 포함

Python build_alert_event 반환 메시지에 schemaVersion: "v1" 및 top-level runId 포함

Pipeline 테스트가 새 계약을 검증하고 통과

Implementation Plan

공통 엔벨로프 스키마 추가

신규 파일: [shared/event-schemas/schemas/event-envelope.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/event-envelope.json)

핵심 규칙:

required: eventType, assetId, timestamp, schemaVersion, payload

optional: runId

schemaVersion은 문자열(v1 현재값)

additionalProperties: false로 top-level 필드 제어

기존 이벤트 스키마 3종을 envelope + payload 확장 구조로 재작성

대상:

[shared/event-schemas/schemas/asset.created.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.created.json)

[shared/event-schemas/schemas/asset.health.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/asset.health.updated.json)

[shared/event-schemas/schemas/alert.generated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/alert.generated.json)

방식:

allOf로 event-envelope.json 참조

각 이벤트별 제약은 properties.eventType.const + properties.payload(이벤트별 payload 스키마)로 덮어쓰기

예시(examples)도 schemaVersion/optional runId 반영

.NET 발행 객체에 schemaVersion 추가

대상: [servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaEventPublisher.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Infrastructure/Kafka/KafkaEventPublisher.cs)

변경:

메시지 객체에 schemaVersion = "v1" 추가

기존 runId 매핑 유지

Pipeline alert event 생성 계약 업데이트

대상: [servers/pipeline/src/pipelines/asset_pipeline.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/src/pipelines/asset_pipeline.py)

변경:

반환 객체 top-level에 schemaVersion: "v1" 추가

top-level runId 추가(있을 때)

호환성 위해 기존 payload.metadata.runId 유지

테스트 보강 및 검증

우선 대상: [servers/pipeline/tests/pipelines/test_asset_pipeline.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/tests/pipelines/test_asset_pipeline.py), [servers/pipeline/tests/workers/test_asset_worker.py](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/pipeline/tests/workers/test_asset_worker.py)

검증 포인트:

schemaVersion == "v1"

runId top-level + payload.metadata.runId 동시 확인

실행:

pipeline pytest 관련 스위트 실행

필요 시 backend 단위 테스트 영향 범위 확인(발행 JSON 필드 assertions 존재 여부 기준)

Why this fits now / later scale

현재는 단일 엔벨로프 스키마 + 코드 필드 추가만으로 drift를 크게 줄이며 복잡도 증가가 작습니다(clarity 우선).

이후 규모 확대 시에는 schemaVersion 기반 다중 버전 공존, CI 호환성 체크(Phase 11.2), 런타임 검증(Phase 10.x)로 자연 확장 가능합니다.
