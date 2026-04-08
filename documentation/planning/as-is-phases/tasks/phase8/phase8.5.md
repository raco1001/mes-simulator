Phase 8.5 API 스키마 OpenAPI 3.x 표준화

Goal

shared/api-schemas/openapi.json을 OpenAPI 3.0.3 형식으로 신규 작성하여 REST API 계약의 단일 기준으로 사용합니다.

기존 하이브리드 포맷(definitions + 커스텀 paths)은 레거시로 남기고, 참조 기준을 OpenAPI로 전환합니다.

Constraints

런타임 코드 변경 없이 계약 문서 정합성 중심으로 진행합니다.

기존 assets.json, state.json, relationships.json은 삭제하지 않고 유지합니다.

현재 백엔드 컨트롤러 기준 엔드포인트/DTO를 우선 반영하고, 익명 에러 응답은 공통 ErrorResponse 스키마로 표준화합니다.

Acceptance Criteria

[shared/api-schemas/openapi.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/openapi.json) 신규 생성 (OpenAPI 3.0.3)

components/schemas에 주요 DTO/Request 정의 포함:

AssetDto, StateDto, RelationshipDto, CreateAssetRequest, UpdateAssetRequest, CreateRelationshipRequest, UpdateRelationshipRequest, AlertDto

RunSimulationRequest, RunResult, StartContinuousRunResult, StopSimulationRunResult, ReplayRunResult, EventDto, ErrorResponse

paths에 실제 API 포함:

/api/assets, /api/assets/{id}

/api/states, /api/states/{assetId}

/api/relationships, /api/relationships/{id}

/api/alerts

/api/simulation/runs, /api/simulation/runs/start, /api/simulation/runs/{runId}/stop, /api/simulation/runs/{runId}/events, /api/simulation/runs/{runId}/replay

[shared/api-schemas/README.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/README.md)에 레거시 안내 + openapi.json 우선 참조 명시

OpenAPI 유효성 검증 통과(가능한 툴 기준)

Implementation Steps

OpenAPI 3.0.3 문서 골격 생성

파일: [shared/api-schemas/openapi.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/openapi.json)

구성:

openapi: "3.0.3"

info (title, version, description)

paths, components.schemas

DTO/Request 스키마 통합

소스 기준:

기존 레거시 스키마: [shared/api-schemas/assets.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/assets.json), [state.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/state.json), [relationships.json](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/relationships.json)

백엔드 DTO: servers/backend/DotnetEngine/Application/\*_/Dto/_.cs

작업:

중복 모델(StateDto) 단일화

nullable/enum/array/object 규칙을 OAS3 표현으로 정리

시뮬레이션 응답 DTO 및 EventDto 추가

경로/응답 매핑

소스 기준:

AssetController, StateController, RelationshipController, AlertController, SimulationController

작업:

메서드/경로별 requestBody, path/query parameter, response schema 정의

400 익명 오류는 ErrorResponse 참조로 통일

README 전환

파일: [shared/api-schemas/README.md](/home/orca/devs/projects/shadow-boxing/Scenario4/shared/api-schemas/README.md)

작업:

openapi.json이 현재 기준 계약임을 명시

기존 3개 JSON은 레거시 유지/호환 목적임을 안내

검증 기반 확보

가능한 경우 수행:

JSON 형식 검증(python -m json.tool)

OpenAPI lint(npx @redocly/openapi-cli lint) 또는 swagger 비교 준비

참고(개발환경): backend swagger endpoint /swagger/v1/swagger.json 과 diff 가능한 절차를 README 또는 노트에 간단히 남김

Why this fits now

계약 문서를 표준 OpenAPI로 단일화하면 거버넌스 툴 파싱과 후속 자동 검증(Phase 10~11) 기반이 즉시 확보됩니다.

런타임 코드를 건드리지 않고도 문서-코드 드리프트를 크게 줄일 수 있습니다.
