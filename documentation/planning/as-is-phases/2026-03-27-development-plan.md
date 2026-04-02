# 개발 계획 (2026-03-27)

본 문서는 Phase 8-9 완료 후 도출된 구조적 인사이트를 바탕으로, **에셋 속성 타입 시스템**과 **시뮬레이션 엔진 확장**을 위한 개발 계획을 정리한 것입니다.

---

## 1. 배경

### 프로젝트 본래 목표

> "임의의 속성을 가진 에셋들이 최소한의 규약에 맞게 다른 에셋과 관계를 맺고 시뮬레이션하는 것"

Phase 1에서 에셋을 `type` + `metadata`(자유 속성)로 일반화했고, 관계(Relationship)를 1급 엔티티로 설계하여 `relationshipType` + `properties`를 자유롭게 정의할 수 있게 했다. 시뮬레이션 엔진(Phase 3~6)은 BFS 기반 전파 + 관계 타입별 규칙(`IPropagationRule`)으로 동작한다.

### 현재 상태 진단

에셋과 관계의 **구조**는 일반화되어 있지만, **상태(State)**와 **시뮬레이션 행동**은 일반화되지 않았다.

#### 문제 1: 하드코딩된 상태 필드

`StateDto`와 `StatePatchDto`가 `CurrentTemp`, `CurrentPower`를 고정 필드로 가진다:

```csharp
// 현재 StatePatchDto — 모든 에셋이 온도와 전력만 가짐
public sealed record StatePatchDto
{
    public double? CurrentTemp { get; init; }
    public double? CurrentPower { get; init; }
    public string? Status { get; init; }
    public string? LastEventType { get; init; }
}
```

- freezer에게는 `CurrentTemp`가 의미 있지만, 배터리에게는 `StoredEnergy`가 필요하다.
- 컨베이어에게는 `Speed`가, 센서에게는 `ReadingValue`가 필요하다.
- 현재 구조에서는 이런 속성을 `Metadata` 딕셔너리에 넣을 수 있지만, 시뮬레이션 엔진이 이를 인식하지 못한다.

#### 문제 2: 물리량 성격을 구분하지 않는 시뮬레이션

시뮬레이션 엔진(`RunSimulationCommandHandler`)이 모든 속성을 동일하게 "패치 덮어쓰기"로 처리한다. 그러나 실제 물리량은 성격에 따라 시뮬레이션 방법이 근본적으로 다르다:

| 성격 | 예시 | 필요한 계산 | 현재 엔진 |
| --- | --- | --- | --- |
| 불변 | 무게, 고정 부피 | 변경 불가 | 구분 없이 덮어쓰기 |
| 설정값 | 목표 온도, 설정 속도 | 외부 이벤트로만 변경 | 구분 없이 덮어쓰기 |
| 변화량 | 속도, 전력 소비 | `value += rate × Δt` | 구분 없이 덮어쓰기 |
| 누적량 | 배터리 잔량, 저장 전력 | `stored += inflow - outflow` (min/max 제한) | 구분 없이 덮어쓰기 |
| 파생값 | 효율, 사용률 | 다른 속성들로부터 계산 | 지원 안 함 |

#### 문제 3: 관계 전파의 불투명성

`IPropagationRule`이 관계 타입만 보고 전파를 결정한다. "어떤 속성을" "어떤 비율로" 전달하는지에 대한 명시적 모델이 없다. `SuppliesRule`이 `CurrentTemp`와 `CurrentPower`를 그대로 복사하는 것이 전부다.

### 목표

1. **State를 동적 Key-Value로 전환** — 에셋별 자유 속성, 하드코딩 필드 제거
2. **PropertyDefinition 도입** — 에셋 타입별 속성 메타 (mutability, simulationBehavior, baseValue)
3. **SimulationBehavior별 엔진 전략** — 속성의 물리적 성격에 따라 tick 계산 분리
4. **관계 기반 속성 흐름 정교화** — 어떤 속성을 어떤 비율로 전달할지 관계 단위 제어

### 설계 원칙

- **Clarity > Complexity**: 각 Phase가 독립적으로 동작 가능해야 한다. Phase 10.1만 적용해도 기존보다 나은 상태여야 한다.
- **Monolith-first**: 별도 서비스나 라이브러리 분리 없이 기존 백엔드 프로젝트 내에서 확장한다.
- **기존 동작 유지**: 각 Phase에서 기존 시뮬레이션이 깨지지 않아야 한다. 마이그레이션 경로를 명시한다.

### 제약

- 기존 BFS 전파 구조(`RunSimulationCommandHandler`)와 관계 기반 규칙(`IPropagationRule`)은 유지
- 프론트엔드 캔버스(React Flow 기반)의 동적 속성 렌더링 대응 필요
- MongoDB 스키마 변경 시 기존 데이터 마이그레이션 고려

---

## 2. 속성 분류 체계 (SimulationBehavior)

Phase 10 전체의 핵심 개념. 각 속성이 시뮬레이션에서 어떻게 행동하는지를 분류한다.

| SimulationBehavior | 설명 | mutability | tick 시 계산 | 예시 |
| --- | --- | --- | --- | --- |
| `constant` | 시뮬레이션 중 절대 변하지 않음 | immutable | 없음 (패치 거부) | 무게, 고정 부피, 정격 전력 |
| `settable` | 외부 이벤트(패치)로만 변경, 자동 변화 없음 | mutable | 없음 (패치만 수용) | 목표 온도, 설정 속도, 가변 부피 |
| `rate` | 매 tick마다 변화량(delta)이 적용됨 | mutable | `value += delta × Δt` | 속도, 전력 소비량, 온도 변화 |
| `accumulator` | 초기 저장량에서 rate에 의해 증감 (min/max 제한) | mutable | `stored += inflow - outflow` | 배터리 잔량, 저장 전력, 탱크 수위 |
| `derived` | 다른 속성들로부터 순수 함수로 계산 | computed | 의존 속성 변경 시 재계산 | 효율 = output/input, 사용률 |

### PropertyDefinition 스키마

```json
{
  "key": "storedEnergy",
  "dataType": "number",
  "unit": "Wh",
  "mutability": "mutable",
  "simulationBehavior": "accumulator",
  "baseValue": 5000,
  "constraints": { "min": 0, "max": 10000 },
  "description": "배터리 저장 에너지"
}
```

### AssetTypeSchema 구조

에셋 타입별로 PropertyDefinition 배열을 관리:

```json
{
  "assetType": "battery",
  "displayName": "배터리",
  "properties": [
    { "key": "weight", "dataType": "number", "unit": "kg", "simulationBehavior": "constant", "baseValue": 50 },
    { "key": "storedEnergy", "dataType": "number", "unit": "Wh", "simulationBehavior": "accumulator", "baseValue": 5000, "constraints": { "min": 0, "max": 10000 } },
    { "key": "chargeRate", "dataType": "number", "unit": "W", "simulationBehavior": "rate", "baseValue": 0 },
    { "key": "efficiency", "dataType": "number", "unit": "%", "simulationBehavior": "derived", "baseValue": 95 }
  ]
}
```

---

## 3. 계획 범위

| Phase | 제목 | 주요 변경 | 영향 범위 |
| --- | --- | --- | --- |
| 10.0 | 개념 모델 확정 (설계) | 문서 + shared 스키마 초안 | 코드 변경 없음 |
| 10.1 | State 동적 Key-Value 전환 | StateDto, StatePatchDto, MergeState, API 스키마, 프론트엔드 | 전 계층 |
| 10.2 | PropertyDefinition 도입 | AssetTypeSchema, MongoDB 컬렉션, 에셋 생성 시 초기화, 검증 | 백엔드 + DB |
| 10.3 | SimulationBehavior 기반 엔진 확장 | IPropertySimulator, Handler 리팩토링 | 백엔드 시뮬레이션 |
| 10.4 | 관계 기반 속성 흐름 정교화 | Relationship 흐름 규칙, IPropagationRule 확장 | 백엔드 시뮬레이션 |

---

## 4. Phase 10.0 — 개념 모델 확정 (설계)

- **목표**: 속성 분류 체계와 PropertyDefinition 스키마를 확정. 코드 변경 전에 설계를 합의.
- **계획**:
  - 본 문서의 "2. 속성 분류 체계" 섹션을 최종 확정
  - `shared/api-schemas/property-definition.json` — PropertyDefinition JSON Schema 초안
  - `shared/api-schemas/asset-type-schema.json` — AssetTypeSchema JSON Schema 초안
  - 예시 에셋 타입 3개 정의 (freezer, battery, conveyor) — 각각의 PropertyDefinition 작성
  - `documentation/backend/property-type-system.md` — 설계 문서 (이 문서의 섹션 2를 상세화)
- **완료 기준**: PropertyDefinition 스키마가 shared에 존재하고, 3개 에셋 타입 예시가 정의됨. 시뮬레이션 엔진 변경 없이 "이 구조에서 어떻게 계산할 것인가"가 문서로 설명됨.
- **산출물**: `property-definition.json`, `asset-type-schema.json`, 에셋 타입 예시 3건, 설계 문서.

---

## 5. Phase 10.1 — State 동적 Key-Value 전환

- **목표**: 하드코딩된 `CurrentTemp`/`CurrentPower`를 제거하고, 에셋마다 다른 속성을 동적으로 가질 수 있게 함. 이 Phase만 적용해도 기존보다 유연한 상태.
- **배경**: 현재 `StateDto`에 `CurrentTemp`, `CurrentPower`가 고정되어 있다. `Metadata` 딕셔너리가 존재하지만 시뮬레이션 엔진이 이를 활용하지 않는다.

### 백엔드 변경

- **StateDto 변경**:
  - `CurrentTemp`, `CurrentPower` 제거
  - `Properties: IReadOnlyDictionary<string, object>` 추가 (기존 `Metadata`를 승격)
  - `Status`, `LastEventType`, `UpdatedAt`은 유지 (시스템 필드)
- **StatePatchDto 변경**:
  - `CurrentTemp`, `CurrentPower` 제거
  - `Properties: IReadOnlyDictionary<string, object?>` 추가
- **MergeState 로직**: 딕셔너리 머지로 변경 — 패치에 있는 키만 덮어쓰기
- **PropagationContext**: `IncomingPatch`가 `StatePatchDto`를 사용하므로 자동 반영
- **IPropagationRule 구현체** (`SuppliesRule`, `ConnectedToRule`, `ContainsRule`):
  - `CurrentTemp`/`CurrentPower` 직접 참조 → `Properties` 딕셔너리 참조로 변경
  - `SuppliesRule`은 `FromState.Properties` 전체를 복사하는 방식으로 단순화 (Phase 10.4에서 선택적 전달로 정교화)
- **이벤트 Payload**: `RunSimulationCommandHandler`의 노드 이벤트 payload에서 `temperature`, `power` → `Properties` 딕셔너리 전체를 포함
- **MongoStateDocument**: `CurrentTemp`, `CurrentPower` → `Properties: BsonDocument` 변환
- **마이그레이션**: 기존 MongoDB `states` 컬렉션의 `currentTemp`/`currentPower` 필드를 `properties.currentTemp`/`properties.currentPower`로 이관하는 init-script 추가

### API 스키마 변경

- `shared/api-schemas/state.json` 및 `openapi.json`의 `StateDto`:
  - `currentTemp`, `currentPower` 제거
  - `properties: { type: "object", additionalProperties: true }` 추가
- 이벤트 스키마 `simulation.state.updated.json`의 `NodeUpdatePayload`:
  - `temperature`, `power` → `properties` 객체로 교체

### 프론트엔드 변경

- 캔버스 페이지 (`AssetsCanvasPage.tsx`): `state.currentTemp` / `state.currentPower` → `state.properties` 기반 동적 렌더링
- 상태 표시: `Object.entries(state.properties)`를 순회하여 key-value 쌍으로 렌더링

### 테스트

- 기존 `RunSimulationCommandHandlerTests`: `StatePatchDto` 생성 방식 변경
- `SuppliesRule`/`ContainsRule`/`ConnectedToRule` 테스트: `Properties` 딕셔너리 기반으로 전환
- 프론트엔드 테스트: 동적 속성 렌더링 검증

- **완료 기준**: 하드코딩 필드가 제거되고, 에셋 상태가 동적 key-value로 저장·조회·전파됨. 기존 시뮬레이션 동작 유지 (키 이름만 변경).
- **산출물**: `StateDto.cs`, `StatePatchDto.cs`, `MongoStateDocument.cs`, `RunSimulationCommandHandler.cs`, 3개 Rule 수정, API 스키마, 프론트엔드, 마이그레이션 스크립트, 테스트.

---

## 6. Phase 10.2 — PropertyDefinition 도입 (에셋 타입별 속성 스키마)

- **목표**: 에셋 타입이 "어떤 속성들을 가지는지" 선언. 에셋 생성 시 속성이 자동으로 초기화되고, 불변 속성은 패치가 거부됨.
- **배경**: Phase 10.1에서 State가 동적 key-value가 되었지만, "어떤 키가 유효한지"에 대한 검증이 없다. 아무 키나 넣을 수 있어 typo로 인한 버그 가능.

### 도메인

- **PropertyDefinition** (Value Object): `Key`, `DataType`, `Unit?`, `Mutability` (immutable/mutable), `SimulationBehavior` (constant/settable/rate/accumulator/derived), `BaseValue`, `Constraints?` (min, max)
- **AssetTypeSchema** (Entity): `AssetType` (string, unique), `DisplayName`, `Properties` (IReadOnlyList<PropertyDefinition>), `CreatedAt`, `UpdatedAt`

### Application 포트

- **Driving**: `ICreateAssetTypeSchemaCommand`, `IGetAssetTypeSchemasQuery`
- **Driven**: `IAssetTypeSchemaRepository` — CRUD + `GetByAssetType(string assetType)`

### Infrastructure

- MongoDB `asset_type_schemas` 컬렉션
- `MongoAssetTypeSchemaDocument` + `MongoAssetTypeSchemaRepository`
- `init-collections.js`에 컬렉션 생성 + 예시 데이터 (freezer, battery, conveyor)

### 에셋 생성 시 초기화

- `CreateAssetCommandHandler` 수정:
  - 에셋 `type`에 해당하는 `AssetTypeSchema` 조회
  - 스키마가 존재하면 `PropertyDefinition.BaseValue`로 초기 `State.Properties` 자동 생성
  - 스키마가 없으면 기존 동작 유지 (자유 속성)

### 불변 속성 검증

- `StatePatchDto` 적용 시, 대상 에셋의 `AssetTypeSchema`를 조회하여 `mutability: immutable`인 키에 대한 패치를 거부
- 거부 시 이벤트 로그에 경고 기록 (시뮬레이션은 중단하지 않고 해당 키만 무시)

### API

- `GET /api/asset-type-schemas` — 전체 목록
- `GET /api/asset-type-schemas/{assetType}` — 특정 타입
- `POST /api/asset-type-schemas` — 생성
- `PUT /api/asset-type-schemas/{assetType}` — 수정

### 프론트엔드

- 에셋 생성 모달: `type` 선택 시 해당 스키마의 속성 목록 표시, `baseValue` 편집 가능
- 에셋 편집 패널: `immutable` 속성은 읽기 전용 표시

- **완료 기준**: 에셋 타입별 속성 스키마가 정의·저장되고, 에셋 생성 시 초기 속성이 자동 세팅됨. 불변 속성 패치 거부.
- **산출물**: Domain 타입, Application 포트·핸들러, Infrastructure 리포지토리, API 컨트롤러, 프론트엔드 모달 수정, 테스트.

---

## 7. Phase 10.3 — SimulationBehavior 기반 엔진 확장

- **목표**: 속성의 `SimulationBehavior`에 따라 tick 계산 방법을 분리. 기존 `IPropagationRule` (관계 기반 전파)과 새 `IPropertySimulator` (속성별 tick 계산)의 **관심사 분리**가 핵심.
- **배경**: Phase 10.2까지 "어떤 속성이 어떤 행동을 하는지" 선언은 가능하지만, 시뮬레이션 엔진이 이를 활용하지 않는다. 모든 속성이 여전히 "패치 덮어쓰기"로만 동작한다.

### 아키텍처

```
SimulationEngineService (tick 루프)
  │
  ├─ RunSimulationCommandHandler (BFS 전파)
  │    │
  │    ├─ 각 노드에서: IPropertySimulator로 속성별 tick 계산
  │    │    ├─ ConstantSimulator: 아무것도 안 함
  │    │    ├─ SettableSimulator: 패치가 있을 때만 적용
  │    │    ├─ RateSimulator: value += delta × Δt
  │    │    ├─ AccumulatorSimulator: stored += inflow - outflow (min/max)
  │    │    └─ DerivedSimulator: 의존 속성으로부터 재계산
  │    │
  │    └─ 관계 전파 시: IPropagationRule로 전달할 속성 결정
  │         ├─ ConnectedToRule: 패치 그대로 전달
  │         ├─ SuppliesRule: 공급 속성 전달
  │         └─ ContainsRule: 포함 관계 전파
  │
  └─ EngineStateApplier (UpsertState → AppendEvent → Publish)
```

### 새 인터페이스

```csharp
public interface IPropertySimulator
{
    SimulationBehavior Behavior { get; }
    object? Compute(PropertySimulationContext ctx);
}

public record PropertySimulationContext
{
    public required PropertyDefinition Definition { get; init; }
    public object? CurrentValue { get; init; }
    public object? PatchValue { get; init; }
    public TimeSpan DeltaTime { get; init; }
    public IReadOnlyDictionary<string, object> AllProperties { get; init; }
}
```

### 구현체

- **ConstantSimulator**: `CurrentValue`를 그대로 반환. `PatchValue`가 있어도 무시 (Phase 10.2의 검증과 이중 보호).
- **SettableSimulator**: `PatchValue ?? CurrentValue` 반환.
- **RateSimulator**: `CurrentValue + (PatchValue ?? Definition.BaseValue) × DeltaTime.TotalSeconds` 반환. `PatchValue`는 delta 값.
- **AccumulatorSimulator**: `CurrentValue + inflow - outflow` 계산 후 `min`/`max` 클램핑. inflow/outflow는 관계로부터 전달받은 값.
- **DerivedSimulator**: `Definition`에 정의된 수식(또는 의존 키 목록)으로 `AllProperties`에서 계산. 초기 구현은 간단한 키 참조만 지원.

### Handler 리팩토링

- `RunSimulationCommandHandler.RunOnePropagationAsync`의 노드 처리 블록에서:
  1. 대상 에셋의 `AssetTypeSchema` 조회
  2. 각 `PropertyDefinition`에 대해 해당 `IPropertySimulator.Compute()` 호출
  3. 결과를 `StateDto.Properties`에 반영
  4. 기존 `MergeState` → `ComputeState`로 교체

### DI 등록

- `IPropertySimulator` 구현체들을 `IEnumerable<IPropertySimulator>`로 DI 등록
- `SimulationBehavior` → `IPropertySimulator` 매핑은 DI에서 자동 해결

- **완료 기준**: `rate` 속성이 매 tick마다 `value += delta × Δt`로 변화하고, `accumulator` 속성이 min/max 내에서 증감하며, `constant` 속성은 절대 변하지 않음. 기존 `IPropagationRule` 동작 유지.
- **산출물**: `IPropertySimulator.cs`, 5개 구현체, `PropertySimulationContext.cs`, `RunSimulationCommandHandler` 리팩토링, DI 등록, 테스트.

---

## 8. Phase 10.4 — 관계를 통한 속성 흐름 규칙 정교화

- **목표**: 관계가 "어떤 속성을 어떤 비율로 전달하는지" 명시적으로 선언. `IPropagationRule`이 이 선언을 읽어 선택적 전달.
- **배경**: Phase 10.3까지 각 노드의 속성은 behavior에 따라 계산되지만, 관계를 통한 전파는 여전히 "전체 속성 복사" 또는 "관계 타입 기반 하드코딩"이다.

### Relationship 흐름 규칙

`RelationshipDto.properties`에 흐름 규칙 필드 추가:

```json
{
  "fromAssetId": "generator-1",
  "toAssetId": "battery-1",
  "relationshipType": "Supplies",
  "properties": {
    "transfers": [
      { "key": "power", "ratio": 0.95, "as": "chargeRate" }
    ]
  }
}
```

- `transfers`: 전달할 속성 목록
  - `key`: 소스 에셋의 속성 키
  - `ratio`: 전달 비율 (0.0 ~ 1.0, 기본 1.0)
  - `as`: 대상 에셋에서의 속성 키 (이름 매핑, 기본: 동일 키)

### IPropagationRule 확장

- `SuppliesRule.Apply()`:
  1. `ctx.Relationship.Properties`에서 `transfers` 배열 파싱
  2. `transfers`가 정의되어 있으면: 지정된 키만 `ratio`를 곱해 `OutgoingPatch`에 포함
  3. `transfers`가 없으면: 기존 동작 유지 (전체 속성 복사) — 하위 호환
- `ConnectedToRule`, `ContainsRule`도 동일 패턴 적용

### 순환 참조 핸들링 고도화

- 현재 BFS `visited` HashSet으로 단순 처리
- Phase 10.4에서는 accumulator 속성의 양방향 흐름(충전 ↔ 방전)을 지원하기 위해:
  - 같은 tick 내에서 이미 방문한 노드에 대해 "누적 패치만 허용" 옵션 추가
  - 순환 감지 시 최대 반복 횟수(기본 1) 내에서 수렴 여부 체크

- **완료 기준**: `Supplies` 관계에서 `transfers` 규칙에 따라 특정 속성만 비율 적용하여 전달됨. 기존 관계(transfers 미정의)는 하위 호환 동작.
- **산출물**: `RelationshipDto` 활용, `SuppliesRule`/`ConnectedToRule`/`ContainsRule` 수정, 순환 참조 핸들링 개선, 테스트.

---

## 9. 확장 시 변경 예상

현재 규모(단일 인스턴스, 에셋 수십 개)에서는 위 설계로 충분하다. 규모가 커질 때 고려할 사항:

| 규모 변화 | 현재 설계 | 확장 시 변경 |
| --- | --- | --- |
| 에셋 수백~수천 | BFS 전파 동기 처리 | 비동기 + 배치 처리, 전파 깊이 동적 제한 |
| 에셋 타입 수십 종 | MongoDB 컬렉션 직접 조회 | AssetTypeSchema 캐싱 (메모리 또는 Redis) |
| 복잡한 파생 속성 | 단순 키 참조 | 수식 엔진 (expression evaluator) 도입 |
| 다중 인스턴스 | 단일 인스턴스 tick 루프 | 분산 잠금 (Redis) + tick 파티셔닝 |
| 실시간 요구사항 | 1초 tick 간격 | tick 간격 축소 + 이벤트 배치 발행 |

---

## 10. 참고 문서

- [2026-03-25-development-plan.md](2026-03-25-development-plan.md) — 이전 개발 계획 (Phase 8~9, 완료)
- [2026-02-22-development-plan.md](2026-02-22-development-plan.md) — 이전 개발 계획 (Phase 6~7c, UI-1)
- [2026-02-20-development-plan.md](2026-02-20-development-plan.md) — 초기 개발 계획 (Phase 1~5)
- [governance-roadmap.md](governance-roadmap.md) — 거버넌스 프로젝트 연결 로드맵 (계약 검증, 스키마 버전 관리, CI)
- [simulation-engine-architecture.md](../backend/simulation-engine-architecture.md) — Simulation 모듈 구조
- [simulation-engine-tick-rules.md](../backend/simulation-engine-tick-rules.md) — 에셋 tick 스키마·due 엔진 규칙
- [event-types.md](../shared/event-types.md) — 이벤트 타입 Command/Observation 분류

---
