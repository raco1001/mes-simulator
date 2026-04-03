# Phase 13 — LinkTypeSchema 구현 + 전파 정교화 (Layer 3b)

**as-is Phase 10.4를 확장. LinkTypeSchema를 정식 도메인으로 도입하고, transfers/ratio 기반 선택적 전파를 구현한다.**

---

## 1. 목표

1. `LinkTypeSchema`를 정식 도메인 개념으로 도입하여 관계 타입별 메타데이터(방향, 시간성, 제약, 흐름 규칙)를 선언
2. `IPropagationRule`이 `LinkTypeSchema`의 `transfers` 규칙을 읽어 선택적 속성 전달
3. 순환 참조 핸들링 고도화

### as-is 대비 변경

| 항목 | as-is (Phase 10.4) | to-be 추가분 |
| --- | --- | --- |
| 흐름 규칙 | `RelationshipDto.properties`에 비정형 저장 | `LinkTypeSchema` 정식 스키마에서 관리 |
| 타입 제약 | 없음 | `fromConstraint` / `toConstraint` (traits 기반) |
| Direction | 암묵적 (from → to) | 명시적 `directed` / `bidirectional` / `hierarchical` |
| Temporality | 없음 | `permanent` / `durable` / `event_driven` |

---

## 2. 선행 조건

- Phase 11 완료 (ObjectTypeSchema + traits 존재)
- Phase 12와 병렬 진행 가능

---

## 3. LinkTypeSchema 도메인

### 3.1 도메인 모델

```csharp
public sealed record LinkTypeSchema
{
    public required string LinkType { get; init; }
    public required string DisplayName { get; init; }
    public required LinkDirection Direction { get; init; }
    public required LinkTemporality Temporality { get; init; }
    public LinkConstraint? FromConstraint { get; init; }
    public LinkConstraint? ToConstraint { get; init; }
    public IReadOnlyList<PropertyDefinition> Properties { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record LinkConstraint
{
    public ObjectTraits? RequiredTraits { get; init; }
    public IReadOnlyList<string>? AllowedObjectTypes { get; init; }
}
```

**닫힌 체계 Enums** (Phase 10에서 정의)

```csharp
public enum LinkDirection { Directed, Bidirectional, Hierarchical }
public enum LinkTemporality { Permanent, Durable, EventDriven }
```

### 3.2 Application 포트

**Driving**

- `ICreateLinkTypeSchemaCommand` → `CreateLinkTypeSchemaHandler`
- `IUpdateLinkTypeSchemaCommand` → `UpdateLinkTypeSchemaHandler`
- `IGetLinkTypeSchemasQuery` → `GetLinkTypeSchemasHandler`
- `IGetLinkTypeSchemaQuery` → `GetLinkTypeSchemaHandler`

**Driven**

- `ILinkTypeSchemaRepository`
  - `CreateAsync(LinkTypeSchema)`
  - `UpdateAsync(LinkTypeSchema)`
  - `GetAllAsync()` → `IReadOnlyList<LinkTypeSchema>`
  - `GetByLinkTypeAsync(string linkType)` → `LinkTypeSchema?`

### 3.3 Infrastructure

- MongoDB `link_type_schemas` 컬렉션
- `MongoLinkTypeSchemaDocument` + `MongoLinkTypeSchemaRepository`
- `init-collections.js` 업데이트: 기존 관계 타입을 LinkTypeSchema로 등록

**초기 데이터 예시**

```json
[
  {
    "linkType": "Supplies",
    "displayName": "공급",
    "direction": "directed",
    "temporality": "durable",
    "fromConstraint": { "requiredTraits": { "dynamism": "dynamic" } },
    "toConstraint": { "requiredTraits": { "dynamism": "dynamic" } },
    "properties": [
      { "key": "transfers", "dataType": "array", "baseValue": [] },
      { "key": "ratio", "dataType": "number", "baseValue": 1.0, "constraints": { "min": 0, "max": 1 } }
    ]
  },
  {
    "linkType": "ConnectedTo",
    "displayName": "연결",
    "direction": "bidirectional",
    "temporality": "durable",
    "properties": []
  },
  {
    "linkType": "Contains",
    "displayName": "포함",
    "direction": "hierarchical",
    "temporality": "permanent",
    "properties": []
  }
]
```

### 3.4 API

| Method | Path | 설명 |
| --- | --- | --- |
| `GET` | `/api/link-type-schemas` | 전체 목록 |
| `GET` | `/api/link-type-schemas/{linkType}` | 특정 타입 조회 |
| `POST` | `/api/link-type-schemas` | 생성 |
| `PUT` | `/api/link-type-schemas/{linkType}` | 수정 |

---

## 4. 관계 생성 시 제약 검증

관계(Relationship) 생성 시 `LinkTypeSchema`의 `fromConstraint` / `toConstraint`를 검증:

1. from/to 에셋의 `ObjectTypeSchema.traits` 조회
2. `requiredTraits`가 설정된 경우 → traits 매칭 확인
3. `allowedObjectTypes`가 설정된 경우 → objectType 포함 확인

**MVP 정책**: 제약 불충족 시 **경고 로그만** (생성 거부 안 함). 확장 시 strict 모드 추가 가능.

---

## 5. transfers 기반 선택적 전파

### 5.1 Relationship 흐름 규칙

`RelationshipDto.properties`에 `transfers` 배열이 존재하면 선택적 전파, 미존재하면 기존 동작 유지.

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

| 필드 | 설명 | 기본값 |
| --- | --- | --- |
| `key` | 소스 에셋의 속성 키 | (필수) |
| `ratio` | 전달 비율 (0.0 ~ 1.0) | 1.0 |
| `as` | 대상 에셋에서의 속성 키 (이름 매핑) | `key`와 동일 |

### 5.2 IPropagationRule 수정

**SuppliesRule.Apply()**

```
1. ctx.Relationship.Properties에서 "transfers" 배열 파싱
2. transfers 정의됨:
   → 지정된 key만 ratio를 곱해 OutgoingPatch.Properties에 포함
   → as가 지정되면 키 이름 변환
3. transfers 미정의:
   → 기존 동작 유지 (전체 Properties 복사) — 하위 호환
```

**ConnectedToRule / ContainsRule**

동일 패턴 적용. `ConnectedTo`는 기본적으로 transfers가 비어 있으므로 패치 그대로 전달. `Contains`는 hierarchical 특성에 맞게 환경 속성(온도, 습도 등)만 전파하도록 선택적 설정 가능.

### 5.3 LinkTypeSchema defaults → Relationship 인스턴스

LinkTypeSchema에 정의된 `properties[].baseValue`가 관계 인스턴스 생성 시 기본 transfers 템플릿으로 사용됨. 사용자가 인스턴스별로 오버라이드 가능.

---

## 6. 순환 참조 핸들링 고도화

### 현재 (as-is)

BFS `visited` HashSet으로 단순 처리. 한번 방문한 노드는 다시 방문하지 않음.

### 확장

accumulator 속성의 양방향 흐름(충전 ↔ 방전)을 지원하기 위해:

1. **동일 tick 내 누적 패치 허용**: 이미 방문한 노드에 대해 accumulator 속성의 flow 값만 누적
2. **수렴 체크**: 최대 반복 횟수(기본 1) 내에서 추가 패치가 임계값 미만이면 수렴으로 판정
3. **순환 감지 시 경고 이벤트**: 무한 루프 가능성이 감지되면 이벤트 로그에 기록

---

## 7. 거버넌스

- LinkTypeSchema의 `linkType`은 시스템 내 고유
- `fromConstraint` / `toConstraint` 위반 시 MVP에서는 경고만, strict 모드는 Phase 이후 옵션
- `transfers` 규칙의 `key`는 소스 ObjectTypeSchema에 정의된 속성이어야 함 (MVP에서는 런타임 경고)
- 새 LinkDirection / LinkTemporality 값 추가 시 영향 분석 필요

---

## 8. 테스트

| 테스트 | 검증 내용 |
| --- | --- |
| `LinkTypeSchemaRepositoryTests` | CRUD 동작 |
| `LinkTypeSchemaApiTests` | API 엔드포인트 |
| `SuppliesRuleTests` | transfers 기반 선택적 전파, ratio 적용, as 키 매핑 |
| `SuppliesRuleTests` (하위 호환) | transfers 미정의 시 전체 복사 |
| `ConnectedToRuleTests` | transfers 적용 |
| `ContainsRuleTests` | hierarchical 전파 |
| `RelationshipCreationTests` | constraint 검증 (경고 발생) |
| `CycleHandlingTests` | 순환 참조 시 누적 패치 + 수렴 체크 |

---

## 9. 완료 기준

- [x] `LinkTypeSchema` CRUD API 동작
- [x] `Supplies` 관계에서 `transfers` 규칙에 따라 특정 속성만 비율 적용 전달
- [x] 기존 관계(transfers 미정의)는 하위 호환 동작
- [x] 관계 생성 시 constraint 검증 (경고 로그)
- [x] 순환 참조 핸들링 개선 (누적 패치 + 수렴 체크)
- [x] LinkTypeSchema 초기 데이터 3건 (Supplies, ConnectedTo, Contains) 등록
- [x] 모든 테스트 통과

---

## 10. 산출물

| 산출물 | 변경 유형 |
| --- | --- |
| `LinkTypeSchema.cs` (도메인 모델) | 신규 |
| `LinkConstraint.cs` | 신규 |
| `LinkDirection.cs`, `LinkTemporality.cs` (enums) | 신규 |
| `ILinkTypeSchemaRepository` + Mongo 구현 | 신규 |
| LinkTypeSchema CRUD API Controller | 신규 |
| `SuppliesRule.cs` | 수정 (transfers 기반 선택적 전파) |
| `ConnectedToRule.cs`, `ContainsRule.cs` | 수정 |
| `RunSimulationCommandHandler.cs` (순환 참조) | 수정 |
| `shared/ontology-schemas/link-type-schema.json` | Phase 10에서 생성됨 (메타계층 소스) |
| `init-collections.js` | 수정 |
| 테스트 8건 | 신규/수정 |

---

## 11. 참고

- [Phase 10 — 온톨로지 메타모델](phase-10-ontology-metamodel.md) — LinkTypeSchema 스키마 정의
- [Phase 11 — 동적 State + ObjectTypeSchema](phase-11-dynamic-state-objecttype.md) — traits 기반 검증에 의존
- [as-is Phase 10.4 — 관계 흐름 정교화](../as-is-phases/2026-03-27-development-plan.md#8-phase-104--관계를-통한-속성-흐름-규칙-정교화)
