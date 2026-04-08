Phase 8.6 필드명 매핑 문서 (Kafka ↔ REST)

Goal

Kafka 이벤트 필드와 REST API 필드의 의도적 차이를 단일 문서로 정리해 계약 불일치로 오해하는 문제를 제거합니다.

Constraints

코드 변경 없이 문서 산출물만 추가합니다.

매핑은 현재 계약(이벤트 스키마: shared/event-schemas/, REST 계약: shared/api-schemas/openapi.json)을 기준으로 합니다.

Acceptance Criteria

[shared/FIELD-MAPPING.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/FIELD-MAPPING.md) 신규 생성

최소 매핑 테이블 포함(계획 문서의 4개 행):

timestamp ↔ occurredAt

runId ↔ simulationRunId

eventType ↔ eventType

assetId ↔ assetId

각 매핑에 “왜 다른지” 설계 근거 포함

실제 현재 코드/스키마에서 쓰이는 이름과 일치

Implementation Steps

매핑 대상 확정(현재 계약 기준)

Kafka(엔벨로프): eventType, assetId, timestamp, runId(optional), schemaVersion, payload

근거: shared/event-schemas/schemas/event-envelope.json

REST(이벤트 조회): EventDto의 occurredAt, simulationRunId, eventType, assetId 등

근거: shared/api-schemas/openapi.json의 components.schemas.EventDto

shared/FIELD-MAPPING.md 작성

형식: 계획서 테이블(개념/필드명/설명) + 추가 섹션(주의사항)

포함할 핵심 설계 근거(문서로 명시):

timestamp vs occurredAt: Kafka는 이벤트 시점 중심(스트리밍), REST EventDto는 저장/조회 맥락에서 발생 시각(OccurredAt)을 사용

runId vs simulationRunId: Kafka는 간결 키, REST는 도메인 의미가 드러나는 명칭

문서 링크(선택, 하지만 권장)

shared/api-schemas/README.md 또는 shared/event-schemas/README.md에 FIELD-MAPPING.md 참조 한 줄 추가 여부 검토

(계획서 요구 산출물에는 없으므로, 필요 시만 추가)

Source files

Kafka 이벤트 계약: [shared/event-schemas/schemas/event-envelope.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/event-envelope.json)

Kafka 이벤트 예시: shared/event-schemas/schemas/\*.json

REST 계약: [shared/api-schemas/openapi.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/openapi.json)

Why this fits now

Phase 8에서 스키마 정합화를 진행하는 흐름상, 명명 차이를 문서로 못 박아 drift/오해를 줄이는 것이 가장 저비용입니다.
