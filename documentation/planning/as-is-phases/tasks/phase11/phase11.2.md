Phase 17 — Phase 2 (관계 매핑 스키마) 구현 계획

목표

관계에 명시적 속성 매핑(mappings)을 추가해, properties.transfers만 쓰던 Supplies 전파와 병행(하위 호환) 하도록 한다.

계약: shared/api-schemas/openapi.json이 단일 소스이며, C#·TS·문서를 여기에 맞춘다.

현재 상태 요약

RelationshipDto.cs: Properties만 있음.

SuppliesRule.cs: TransferSpecParser.Parse + BuildTransferredProperties만 사용.

MongoRelationshipDocument.cs: properties BSON만 있음.

TransferSpecParser.cs: incoming 패치 우선, 없으면 fromState.Properties를 소스로 사용하는 로직이 이미 있음 — 매핑 경로도 동일한 소스 딕셔너리를 써야 동작이 일관됨.

Task 2-A — OpenAPI

파일: shared/api-schemas/openapi.json

components.schemas에 PropertyMapping 추가 (phase 문서와 동일: fromProperty, toProperty, transformRule 기본 "value", 선택 fromUnit/toUnit).

**RelationshipDto**에 mappings 배열 추가 (items → $ref: PropertyMapping, 기본 [], 설명: 비어 있으면 기존 transfers 방식).

CreateRelationshipRequest, **UpdateRelationshipRequest**에 동일하게 선택 필드 mappings 추가 (업데이트 시 null이면 기존 값 유지할 수 있게 백엔드에서 처리).

required 배열: 기존 필수는 유지하고, mappings는 필수에 넣지 않음(기본 빈 배열).

Task 2-B — 백엔드 DTO 및 핸들러

신규 PropertyMapping 레코드 (권장 위치: Application/Relationship/Dto/ — PropertyMapping.cs 또는 RelationshipDto.cs 인접).

필드: FromProperty, ToProperty, TransformRule 기본 "value", FromUnit/ToUnit nullable.

RelationshipDto.cs: IReadOnlyList<PropertyMapping> Mappings { get; init; } = []; (또는 Array.Empty 등 프로젝트 스타일에 맞게).

CreateRelationshipRequest.cs: Mappings 추가, 기본 빈 목록.

UpdateRelationshipRequest.cs: IReadOnlyList<PropertyMapping>? Mappings (null = 변경 없음).

CreateRelationshipCommandHandler.cs: RelationshipDto 생성 시 Mappings = request.Mappings (또는 빈 배열).

UpdateRelationshipCommandHandler.cs: Mappings = request.Mappings ?? existing.Mappings.

Task 2-B(연속) — Mongo 영속화

파일: MongoRelationshipDocument.cs, MongoRelationshipRepository.cs

BSON용 소형 클래스(예: MongoPropertyMapping)에 [BsonElement("fromProperty")] 등 camelCase 필드명으로 매핑.

MongoRelationshipDocument에 [BsonElement("mappings")] List<MongoPropertyMapping>? Mappings 추가.

ToDto / ToDocument: Mappings 양방향 변환; 문서에 필드 없음 → 빈 리스트.

Properties는 기존과 동일 — 시드/레거시 문서와 호환.

Task 2-C — SuppliesRule

파일: SuppliesRule.cs

Apply 시작 시 소스 프로퍼티 딕셔너리 결정: TransferSpecParser.BuildTransferredProperties와 동일 규칙 (incoming 패치 우선, 없으면 fromState).

중복 방지: TransferSpecParser.cs에 internal static Dictionary<string, object?> ResolveSourceProperties(StatePatchDto incoming, StateDto? fromState) 같은 한 줄짜리 헬퍼를 추가해 두 경로가 공유하도록 하는 것이 안전함.

ctx.Relationship.Mappings is { Count: > 0 }이면:

ApplyMappings(mappings, source)로 Dictionary<string, object?> 생성 (phase 문서의 ApplyTransform: value, value \* N, value / N, 및 문서 스니펫의 +/- 지원).

소스에 키 없으면 해당 매핑 스킵.

숫자 변환: 기존 TryToDouble 로직과 맞추기 위해 TransferSpecParser의 private 파서를 internal로 승격하거나, 동일 시맨틱의 TryCoerceDouble를 같은 어셈블리에서 재사용.

그렇지 않으면 기존 Parse + BuildTransferredProperties 유지.

flowchart LR
subgraph supplies [SuppliesRule.Apply]
A[ResolveSourceProperties]
B{Mappings non-empty?}
C[ApplyMappings + ApplyTransform]
D[TransferSpecParser legacy]
E[Outgoing StatePatchDto]
end
A --> B
B -->|yes| C --> E
B -->|no| D --> E

Task 2-D — MODEL.md

파일: infrastructure/mongo/MODEL.md — ### 4. relationships 절에 phase 문서의 mappings 배열 설명 추가 (필드별 bullet, 비어 있으면 transfers 하위 호환).

프론트엔드 (계약 정합)

Phase 문서에 없으나 CLAUDE.md 규칙상 OpenAPI 변경 후 TS 타입 동기화 권장.

servers/frontend/src/entities/relationship/model/types.ts: PropertyMapping 인터페이스, RelationshipDto / CreateRelationshipRequest / UpdateRelationshipRequest에 mappings? 추가.

UI 폼은 이번 범위에서 필수 아님 (타입·API만 맞춰도 빌드/계약은 일치).

테스트

SuppliesRuleTests.cs: Mappings가 채워진 관계에서 fromProperty → toProperty 전파 및 transformRule (예: value \* 2) 검증; Mappings 비우면 기존 transfers/폴백 동작 유지 케이스 1건.

CreateRelationshipCommandHandlerTests.cs: 요청에 Mappings 포함 시 DTO/저장 호출에 반영되는지 (mock AddAsync 캡처).

RunSimulationCommandHandlerTests.cs 등 new RelationshipDto { ... } 사용처: 컴파일을 위해 Mappings 생략 가능하도록 DTO에 기본값만 두면 대부분 수정 불필요.

검증

dotnet test servers/backend/DotnetEngine.Tests/DotnetEngine.Tests.csproj

(선택) 프론트 npm test — relationship 타입을 참조하는 테스트가 있으면 실행

범위 밖 / 후속

Pipeline Python 관계 DTO: Phase 2 문서에 없음 — Kafka/파이프라인이 relationship 페이로드를 소비하면 별도 작업.

OpenAPI 생성기로 TS 자동 생성하지 않는 현재 구조라면 수동 타입 유지.
