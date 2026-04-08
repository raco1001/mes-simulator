Phase 17 — Task 1 (8–384) 구현 계획

문서 vs 코드베이스 정합 (반드시 반영)

Phase 문서

실제 코드베이스

구현 시 조치

objectTypeSchemas 컬렉션

[MongoObjectTypeSchemaRepository](servers/backend/DotnetEngine/Infrastructure/Mongo/MongoObjectTypeSchemaRepository.cs) → object_type_schemas

시드/Repository는 object_type_schemas 사용

평면 JSON을 그대로 replace_one

문서는 \_id + objectType + payloadJson(임베디드 DTO)

시드 스크립트가 DTO를 payloadJson에 넣고 \_id/objectType 필드 맞춤

motor + async

파이프라인은 동기 pymongo

ObjectTypeRepository·get_asset는 동기로 구현 (문서의 async 예시는 사용하지 않음)

Drone traits: Ephemeral / Multiple

[ontology-common-defs.json](shared/ontology-schemas/ontology-common-defs.json) — Persistence: Permanent/Durable/Transient; Cardinality: Singular/Enumerable/Streaming

Drone JSON은 Transient + Enumerable(또는 문서 의도대로 enum 확장 시 shared 스키마 + C# enum까지 별도 작업)

allowedLinks direction Outgoing

[object-type-schema.json](shared/ontology-schemas/object-type-schema.json) — inbound | outbound | bidirectional

outbound 등으로 수정

LinkType Monitors / OperatedBy

현재 [01-link-type-schemas.js](infrastructure/mongo/seeds/01-link-type-schemas.js)에 없음

(A) Drone의 allowedLinks를 기존 Supplies/ConnectedTo/Contains로 바꾸거나 (B) 동일 Phase에서 링크 시드에 최소 스키마 2개 추가 — 권장: (B) 최소 시드 추가로 Phase 17 의도 유지

구현 단계

Task 1-A — property-definition.json 확장

파일: [shared/ontology-schemas/property-definition.json](shared/ontology-schemas/property-definition.json)

properties에 Phase 문서의 alertThresholds(배열, items: level/condition/value), derivedRule(type linear, timeUnit, inputs[]) 추가.

additionalProperties: false 유지 — 새 키는 properties 안에만 정의.

후속: [validate_phase10_examples.py](shared/ontology-schemas/validate_phase10_examples.py) 및 기존 examples가 깨지면 스키마에 맞게 보정.

Task 1-B — Drone ObjectType 시드 (JSON)

파일: infrastructure/mongo/seeds/drone_objecttype.json (Phase 명칭 유지)

위 정합 표에 맞춰 traits·allowedLinks 수정.

classifications: 없으면 [] 추가 (ObjectType 스키마와 일치).

abstractSchema/extends 선택 필드는 생략 가능 시 생략.

Task 1-C — ObjectType 시드 스크립트 (Python)

파일: infrastructure/mongo/seeds/seed_objecttypes.py

기본 DB 이름: factory_mes (문서의 mes 아님).

기본 URI: mongodb://admin:admin123@localhost:27017/?authSource=admin (로컬 docker와 README 정합; --mongo-uri로 덮어쓰기).

\*\_objecttype.json glob 정렬 후 각 파일:

JSON 로드 → object_type = schema["objectType"]

Mongo 문서: { \_id: object_type, objectType: object_type, payloadJson: { ...dto with ownProperties, traits, ... } }

payloadJson에 createdAt/updatedAt를 datetime.utcnow() 등으로 채워 백엔드 DTO와 유사하게 맞춤 (기존 링크 시드의 ISO 문자열과 혼용 시 BSON 타입만 일관되게).

replace_one({ "objectType": ot }, doc, upsert=True) 권장.

의존성: 이미 [pyproject.toml](servers/pipeline/pyproject.toml)에 pymongo 있음 — 시드만 돌릴 때는 uv run python ... 또는 venv에서 실행 안내.

Task 1-D — ObjectTypeRepository (동기)

파일: [servers/pipeline/src/repositories/mongo/object_type_repository.py](servers/pipeline/src/repositories/mongo/object_type_repository.py) (신규)

Settings + pymongo로 object_type_schemas 접근.

get_by_object_type(object_type: str) -> dict | None: find_one({"objectType": object_type}) 후 payloadJson dict만 반환 (파이프라인은 ownProperties 등이 payload 안에 있음). 문서 없으면 None.

Task 1-E — [asset_pipeline.py](servers/pipeline/src/pipelines/asset_pipeline.py)

\_evaluate_alert_thresholds(properties, schema) 추가 — schema는 payloadJson 루트와 동일한 dict (ownProperties 포함). 반환값을 [AssetConstants.Status](servers/pipeline/src/domains/asset/constants.py)의 normal/warning/error로 매핑 (문서의 warning/error 문자열과 동일 의미).

calculate_state 시그니처: event, asset_type: str = "unknown", schema: dict | None = None. 타입 힌트는 AssetHealthUpdatedEventDto | SimulationStateUpdatedEventDto 또는 Any/Protocol로 통일 (현재 worker가 둘 다 전달).

분기: payload.get("status") 있으면 유지 → elif schema: → \_evaluate_alert_thresholds → else 기존 \_calculate_status_from_properties.

calculate_derived_properties(current_state: dict, schema: dict, delta_seconds: float) -> dict 추가 — Phase 의사코드와 동일; schema는 payload 루트 dict.

Task 1-F — [asset_worker.py](servers/pipeline/src/workers/asset_worker.py) + [asset_repository.py](servers/pipeline/src/repositories/mongo/asset_repository.py)

AssetRepository.get_asset(asset_id: str) -> dict | None — assets 컬렉션 find_one({"\_id": asset_id}), \_id 제외 또는 그대로 반환 후 type 필드 사용.

AssetWorker.**init**: ObjectTypeRepository(settings) 생성 (또는 주입 가능하게 유지).

process_health_updated: get_asset → asset_type → get_by_object_type → calculate_state(event, asset_type=..., schema=schema).

process_simulation_state_updated: 기존 calculate_state(event)로 초기 state 계산 후, schema 있으면 delta_seconds = float(payload.get("deltaSeconds", 1.0)), current = 기존 properties와 병합(문서의 state.**dict** 대신 state.properties 및 이벤트 payload["properties"] 기준), calculate_derived_properties 결과를 properties에 merge → AssetState 불변성을 지키려면 새 AssetState 인스턴스를 만들거나 replace()로 갱신된 properties로 재구성 후 save_state.

주의: [domains/asset](servers/pipeline/src/domains/asset)의 AssetState가 frozen dataclass인지 확인하고, 수정이 필요하면 같은 PR에서 안전하게 처리.

Task 1-G (문서에 없으나 권장) — 링크 타입 시드

Monitors / OperatedBy를 Drone에 유지할 경우 [infrastructure/mongo/seeds/01-link-type-schemas.js](infrastructure/mongo/seeds/01-link-type-schemas.js)에 최소 payloadJson 문서 추가 또는 02-\*.js 분리 시드로 추가해 백엔드/검증과 충돌 없게 맞춤.

테스트 (TDD 권장)

[tests/pipelines/test_asset_pipeline.py](servers/pipeline/tests/pipelines/test_asset_pipeline.py): \_evaluate_alert_thresholds (battery lt 20 → warning 등), calculate_state(..., schema=...), calculate_derived_properties 선형 규칙 1케이스.

ObjectTypeRepository / get_asset: mongomock 또는 통합 테스트는 선택; 최소 단위는 mock Collection.find_one.

백엔드/프론트 (범위 밖 명시)

C# [PropertyDefinition](servers/backend/DotnetEngine/Application/ObjectType/Dto/ObjectTypeSchemaDto.cs)에 필드가 없어도 기본 JSON 역직렬화는 알 수 없는 속성을 무시하는 경우가 많아 API는 동작할 수 있으나, 편집/표시를 위해선 후속으로 DTO·TS 타입 추가 권장. 본 Phase 1 구현 범위에서는 파이프라인 + 스키마 + 시드 우선 완료로 두고 README/Phase 문서에 후속 작업으로 한 줄 기록.

데이터 흐름 (요약)

sequenceDiagram
participant W as AssetWorker
participant AR as AssetRepository
participant OT as ObjectTypeRepository
participant P as asset_pipeline

W->>AR: get_asset(assetId)
AR-->>W: type
W->>OT: get_by_object_type(type)
OT-->>W: payloadJson dict
W->>P: calculate_state(event, schema=payloadJson)
P-->>W: AssetState
W->>AR: save_state

검증

pytest servers/pipeline/tests -v

(선택) seed_objecttypes.py 로컬 Mongo에 실행 후 object_type_schemas에 Drone 문서 확인
