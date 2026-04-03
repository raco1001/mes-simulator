# Phase 11 — 동적 State + ObjectTypeSchema 구현 (Layer 2)

**as-is Phase 10.1 + 10.2 통합. 하드코딩 필드 제거 → 동적 Properties + ObjectTypeSchema(traits/classifications) 도입.**

---

## 1. 목표

1. `StateDto` / `StatePatchDto`의 하드코딩 필드(`CurrentTemp`, `CurrentPower`)를 제거하고 동적 `Properties: Dictionary<string, object>`로 전환
2. `ObjectTypeSchema` 도메인 개념을 도입하여 각 에셋 타입에 `PropertyDefinition[]`, `traits`, `classifications`를 선언
3. 에셋 생성 시 `baseValue` 기반 자동 초기화, `immutable` 속성 패치 거부

### Phase 11만 적용해도 기존보다 나은 이유

- 에셋 타입별 자유 속성 선언 가능 (freezer에는 온도, battery에는 저장량)
- 하드코딩 제거로 새 에셋 타입 추가 시 코드 변경 불필요
- traits를 통해 시스템이 객체 행동을 인지할 수 있는 기반 마련

---

## 2. 선행 조건

- Phase 10 완료 (shared 스키마 확정, 설계 합의)

---

## 3. State 동적 Key-Value 전환 (as-is 10.1 대응)

### 3.1 백엔드 변경

**DTO 변경**

| 파일 | 변경 내용 |
| --- | --- |
| `Application/Simulation/Dto/StatePatchDto.cs` | `CurrentTemp`, `CurrentPower` 제거 → `Properties: IReadOnlyDictionary<string, object?>` 추가 |
| `Application/Simulation/Dto/StateDto.cs` 또는 해당 DTO | `CurrentTemp`, `CurrentPower` 제거 → `Properties: IReadOnlyDictionary<string, object>` 추가 |

`Status`, `LastEventType`, `UpdatedAt`은 시스템 필드로 유지.

**MergeState 로직**

기존: 각 필드를 개별 null-coalesce.
변경: 딕셔너리 머지 — 패치에 존재하는 키만 덮어쓰기.

```csharp
// 의사코드
foreach (var (key, value) in patch.Properties)
{
    if (value is null)
        current.Properties.Remove(key);
    else
        current.Properties[key] = value;
}
```

**PropagationContext**

`IncomingPatch`가 `StatePatchDto`를 사용하므로 자동 반영.

**IPropagationRule 구현체**

| Rule | 변경 |
| --- | --- |
| `SuppliesRule` | `CurrentTemp`/`CurrentPower` 직접 참조 → `Properties` 딕셔너리 전체 복사 (Phase 13에서 선택적 전달로 정교화) |
| `ConnectedToRule` | 동일하게 `Properties` 기반으로 전환 |
| `ContainsRule` | 동일하게 `Properties` 기반으로 전환 |

**이벤트 Payload**

`RunSimulationCommandHandler`의 노드 이벤트 payload:
- `temperature`, `power` 필드 → `properties` 딕셔너리 전체 포함

**MongoDB**

| 변경 | 내용 |
| --- | --- |
| `MongoStateDocument` | `CurrentTemp`, `CurrentPower` 필드 → `Properties: BsonDocument` |
| 마이그레이션 | init-script에 기존 `currentTemp`/`currentPower` → `properties.currentTemp`/`properties.currentPower` 이관 로직 추가 |

### 3.2 API 스키마 변경

- `shared/api-schemas/state.json`: `currentTemp`, `currentPower` 제거 → `properties: { type: "object", additionalProperties: true }` 추가
- `shared/api-schemas/assets.json` 내 `StateDto`: 동일하게 변경
- 이벤트 스키마 `simulation.state.updated.json`: `temperature`, `power` → `properties` 객체

### 3.3 프론트엔드 변경

- `AssetsCanvasPage.tsx`: `state.currentTemp` / `state.currentPower` → `Object.entries(state.properties)`를 순회하여 key-value 동적 렌더링
- 상태 패널: 동적 속성 목록 표시

### 3.4 테스트

- `RunSimulationCommandHandlerTests`: `StatePatchDto` 생성 방식을 딕셔너리 기반으로 변경
- `SuppliesRule` / `ConnectedToRule` / `ContainsRule` 테스트: `Properties` 딕셔너리 기반 전환
- 프론트엔드: 동적 속성 렌더링 스냅샷 테스트
- Pipeline: `calculate_state`/worker/alert 테스트를 `properties-only` + `metrics[]` 계약으로 갱신

---

## 4. ObjectTypeSchema 도입 (as-is 10.2 대응 + traits/classifications 확장)

### 4.1 도메인 모델

**PropertyDefinition** (Value Object)

Phase 10에서 확정한 스키마 그대로 코드로 옮긴다.

```csharp
public sealed record PropertyDefinition
{
    public required string Key { get; init; }
    public required DataType DataType { get; init; }
    public string? Unit { get; init; }
    public required SimulationBehavior SimulationBehavior { get; init; }
    public required Mutability Mutability { get; init; }
    public object? BaseValue { get; init; }
    public PropertyConstraints? Constraints { get; init; }
    public bool Required { get; init; } = true;
}
```

**ObjectTypeSchema** (Entity)

```csharp
public sealed record ObjectTypeSchema
{
    public required string ObjectType { get; init; }
    public required string DisplayName { get; init; }
    public required ObjectTraits Traits { get; init; }
    public IReadOnlyList<Classification> Classifications { get; init; } = [];
    public required IReadOnlyList<PropertyDefinition> Properties { get; init; }
    public IReadOnlyList<AllowedLink> AllowedLinks { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

**ObjectTraits** (Value Object)

```csharp
public sealed record ObjectTraits
{
    public required Persistence Persistence { get; init; }
    public required Dynamism Dynamism { get; init; }
    public required Cardinality Cardinality { get; init; }
}
```

**Classification** (Value Object)

```csharp
public sealed record Classification
{
    public required string Taxonomy { get; init; }
    public required string Value { get; init; }
}
```

**닫힌 체계 Enums** (Phase 10 정의를 코드로 반영)

```csharp
public enum DataType { Number, String, Boolean, DateTime, Array, Object }
public enum SimulationBehavior { Constant, Settable, Rate, Accumulator, Derived }
public enum Mutability { Immutable, Mutable }
public enum Persistence { Permanent, Durable, Transient }
public enum Dynamism { Static, Dynamic, Reactive }
public enum Cardinality { Singular, Enumerable, Streaming }
```

### 4.2 Application 포트

**Driving (Inbound)**

- `ICreateObjectTypeSchemaCommand` → `CreateObjectTypeSchemaHandler`
- `IUpdateObjectTypeSchemaCommand` → `UpdateObjectTypeSchemaHandler`
- `IGetObjectTypeSchemasQuery` → `GetObjectTypeSchemasHandler`
- `IGetObjectTypeSchemaQuery` → `GetObjectTypeSchemaHandler`

**Driven (Outbound)**

- `IObjectTypeSchemaRepository`
  - `CreateAsync(ObjectTypeSchema)`
  - `UpdateAsync(ObjectTypeSchema)`
  - `GetAllAsync()` → `IReadOnlyList<ObjectTypeSchema>`
  - `GetByObjectTypeAsync(string objectType)` → `ObjectTypeSchema?`

### 4.3 Infrastructure

- MongoDB `object_type_schemas` 컬렉션
- `MongoObjectTypeSchemaDocument` + `MongoObjectTypeSchemaRepository`
- `init-collections.js` 업데이트: 컬렉션 생성 + 예시 데이터 3건 (freezer, battery, conveyor)
  - MVP에서 모든 예시 타입의 traits: `{ persistence: "durable", dynamism: "dynamic", cardinality: "enumerable" }`
  - classifications: `[{ taxonomy: "industry", value: "manufacturing.equipment" }]`

### 4.4 에셋 생성 시 초기화

`CreateAssetCommandHandler` 수정:

1. 에셋 `type`에 해당하는 `ObjectTypeSchema` 조회
2. 스키마 존재 시 → `PropertyDefinition.BaseValue`로 초기 `State.Properties` 자동 생성
3. 스키마 미존재 시 → 기존 동작 유지 (자유 속성)

### 4.5 불변 속성 패치 거부

`StatePatchDto` 적용 시:

1. 대상 에셋의 `ObjectTypeSchema` 조회
2. 패치 키 중 `mutability: immutable`인 키 → 해당 키만 무시
3. 거부 이벤트 로그에 경고 기록 (시뮬레이션 중단 없음)

### 4.6 API

| Method | Path | 설명 |
| --- | --- | --- |
| `GET` | `/api/object-type-schemas` | 전체 목록 |
| `GET` | `/api/object-type-schemas/{objectType}` | 특정 타입 조회 |
| `POST` | `/api/object-type-schemas` | 생성 |
| `PUT` | `/api/object-type-schemas/{objectType}` | 수정 |

> **G-4 이행**: 위 엔드포인트를 `shared/api-schemas/openapi.json`에 추가할 때, request/response payload 구조는 `shared/ontology-schemas/object-type-schema.json`을 소스로 동기화해야 한다. 두 파일이 달라지면 스키마 계약 위반이 발생한다.

### 4.7 프론트엔드

- 에셋 생성 모달: `type` 선택 시 해당 ObjectTypeSchema의 속성 목록 표시, `baseValue` 편집 가능
- 에셋 편집 패널: `immutable` 속성은 읽기 전용 표시
- 캔버스 노드: traits 정보를 아이콘/뱃지로 표시 (선택사항, MVP에서는 미구현 가능)

### 4.8 Pipeline 동기 변경

`calculate_state()`의 하드코딩 필드 참조(`currentTemp`, `currentPower`) → 동적 `properties` 딕셔너리 대응으로 변경.
이번 단계에서 파이프라인 입력은 legacy fallback 없이 `payload.properties`만 허용한다.
또한 `alert.generated` payload는 단일 metric 필드가 아니라 `metrics[]` 구조를 사용한다.

---

## 5. 거버넌스

- ObjectTypeSchema 내 PropertyDefinition `key`는 objectType 내에서 고유해야 함 (생성/수정 시 검증)
- `required: true`인 속성은 에셋 생성 시 `baseValue` 또는 명시적 값 필수
- `mutability: immutable` 속성은 생성 후 패치 불가
- traits 3축은 필수 (null 불가)
- API 스키마가 OpenAPI 문서와 일치하는지 검증 (스키마 테스트)

---

## 6. 완료 기준

- [x] `StateDto` / `StatePatchDto`에서 하드코딩 필드 완전 제거
- [x] 에셋 상태가 동적 key-value로 저장·조회·전파됨
- [x] 기존 시뮬레이션 동작 유지 (키 이름만 변경)
- [x] `ObjectTypeSchema` CRUD API 동작
- [x] ObjectTypeSchema에 `traits`, `classifications` 포함
- [x] 에셋 생성 시 baseValue 기반 자동 초기화
- [x] immutable 속성 패치 거부
- [x] MongoDB 마이그레이션 스크립트 동작
- [x] 프론트엔드 동적 속성 렌더링
- [x] Pipeline `calculate_state()` 동적 properties 대응
- [x] 기존 테스트 + 새 테스트 통과 (Frontend Vitest, Pipeline pytest)

### Backend-first 진행 상태

- 완료: DTO/핸들러/전파규칙/Mongo 상태 저장 경로를 `properties` 기반으로 전환
- 완료: `object_type_schemas` 저장소/핸들러/컨트롤러/DI 연결
- 완료: `CreateAsset` 시 ObjectTypeSchema `baseValue` 초기화 및 immutable 패치 가드 반영
- 완료: Frontend `state.properties` 렌더링/Canvas ObjectTypeSchema UX 반영
- 완료: Pipeline `properties-only` 처리 + multi-metric alert payload(`metrics[]`) 반영
- 완료: Phase 12에서 `SimulationBehavior`별 simulator 경로가 연결되어 `ObjectTypeSchema.PropertyDefinition` 계약이 런타임 계산 경로로 확장됨
- 대기: NuGet 네트워크 프록시(403)로 backend `dotnet test` 실행 불가, CI 또는 네트워크 가능한 환경에서 재검증 필요

---

## 7. 산출물

| 산출물 | 변경 유형 |
| --- | --- |
| `StatePatchDto.cs`, `StateDto.cs` | 수정 |
| `MergeState` 로직 (RunSimulationCommandHandler 내) | 수정 |
| `SuppliesRule.cs`, `ConnectedToRule.cs`, `ContainsRule.cs` | 수정 |
| `MongoStateDocument.cs` | 수정 |
| ObjectTypeSchema 도메인 모델 (Entity, VO, Enums) | 신규 |
| `IObjectTypeSchemaRepository` + Mongo 구현 | 신규 |
| ObjectTypeSchema CRUD API Controller | 신규 |
| `CreateAssetCommandHandler` (초기화 로직) | 수정 |
| `shared/api-schemas/state.json`, `assets.json` | 수정 |
| `shared/ontology-schemas/object-type-schema.json` | Phase 10에서 생성됨 (메타계층 소스) |
| `init-collections.js` | 수정 |
| `AssetsCanvasPage.tsx` | 수정 |
| Pipeline `calculate_state()` | 수정 |
| 테스트 전체 | 수정/추가 |

---

## 8. 참고

- [Phase 10 — 온톨로지 메타모델](phase-10-ontology-metamodel.md)
- [as-is Phase 10.1 — State 동적 전환](../as-is-phases/2026-03-27-development-plan.md#5-phase-101--state-동적-key-value-전환)
- [as-is Phase 10.2 — PropertyDefinition](../as-is-phases/2026-03-27-development-plan.md#6-phase-102--propertydefinition-도입-에셋-타입별-속성-스키마)
