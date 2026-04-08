Phase 8.2 simulation.state.updated 스키마 추가

Goal

가장 많이 사용되는 simulation.state.updated 이벤트 계약을 shared/에 공식 스키마로 정의해 SSoT 공백을 제거합니다.

실제 백엔드 발행 payload의 두 변형(노드 업데이트/관계 전파)을 명시적으로 문서화하고 검증 가능한 구조로 만듭니다.

Constraints

기존 동작 보존: 현재 백엔드 발행 필드/타입을 기준으로 스키마를 맞춥니다 (breaking change 금지).

기존 Phase 8.1 엔벨로프 규약 준수: event-envelope.json $ref + eventType const 유지.

복잡도 최소화: 런타임 검증 로직은 이번 단계에서 추가하지 않고 계약 정의/등록에 집중합니다.

Acceptance Tests

[shared/event-schemas/schemas/simulation.state.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/simulation.state.updated.json) 신규 생성

스키마가 event-envelope.json을 $ref(allOf)하고 eventType = simulation.state.updated를 강제

payload가 oneOf로 NodeUpdatePayload / PropagationPayload를 표현

공통 필수 tick, depth(integer)가 두 변형 모두에 존재

[shared/event-schemas/versions/v1.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/versions/v1.json)에 simulation.state.updated 등록

Implementation Steps

신규 스키마 파일 추가

파일: [shared/event-schemas/schemas/simulation.state.updated.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/schemas/simulation.state.updated.json)

구조:

allOf: {"$ref": "event-envelope.json"} + 이벤트 특화 객체

properties.eventType.const = "simulation.state.updated"

properties.payload.oneOf로 2개 정의

payload 변형 정의(실제 코드 기준)

NodeUpdatePayload (RunSimulationCommandHandler 발행):

필수: tick(integer), depth(integer), status(string), temperature(number), power(number)

PropagationPayload (Supplies/Contains/ConnectedToRule 발행):

필수: tick(integer), depth(integer), relationshipType(enum: Supplies|Contains|ConnectedTo), fromAssetId(string)

선택: relationshipId(string)

각 payload 객체에 additionalProperties: false 적용해 drift 방지

버전 매니페스트 등록

파일: [shared/event-schemas/versions/v1.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/versions/v1.json)

schemas 항목에 추가:

"simulation.state.updated": "schemas/simulation.state.updated.json"

notes 문구는 필요 시 최소 보정

문서 정합성 점검

파일: [shared/event-schemas/README.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/event-schemas/README.md)

이벤트 목록/확장 가이드에 simulation.state.updated 추가 (간단 예시 포함)

Source-of-truth code references

Node update payload: [servers/backend/DotnetEngine/Application/Simulation/Handlers/RunSimulationCommandHandler.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Application/Simulation/Handlers/RunSimulationCommandHandler.cs)

Propagation payload: [servers/backend/DotnetEngine/Application/Simulation/Rules/SuppliesRule.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Application/Simulation/Rules/SuppliesRule.cs), [ContainsRule.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Application/Simulation/Rules/ContainsRule.cs), [ConnectedToRule.cs](/home/orca/devs/projects/shadow-boxing/Scenario4/servers/backend/DotnetEngine/Application/Simulation/Rules/ConnectedToRule.cs)

Why this fits now

지금은 계약 정의만 추가해도 shared/-코드 drift를 크게 줄일 수 있고, 기존 서비스 동작에 영향을 주지 않습니다.

이후 Phase 10/11에서 런타임 검증/호환성 체크를 붙일 때 바로 활용 가능한 최소·명확한 기반입니다.
