# Phase 10 — 온톨로지 메타모델 (Layer 1 설계)

**코드 변경 없음. 설계 확정 + shared 스키마 산출물.**

---

## 1. 목표

도메인 무관한 온톨로지 메타모델을 확정한다. 이 메타모델은 제조업뿐 아니라 의료, 물류, 발전, 통신 등 어떤 도메인의 객체도 수용할 수 있는 구조여야 한다.

### 핵심 결정

- 분류 기준을 "무엇인가" (도메인 어휘)에서 "어떻게 행동하는가" (존재론적 특성)로 전환
- 시스템이 읽는 **닫힌 체계**와 사람이 읽는 **열린 체계**를 명확히 분리
- "Asset"이라는 용어는 코드에서 유지하되, 문서에서 "이 시스템의 모든 온톨로지 객체를 지칭"으로 의미 확장

### as-is 대비 변경

| 항목 | as-is (Phase 10.0) | to-be |
| --- | --- | --- |
| 타입 분류 | 없음 (자유 문자열 type) | traits (행동 특성 3축) + classifications (열린 태그) |
| ObjectTypeSchema | `assetType`, `properties[]` | + `traits`, `classifications[]`, `allowedLinks[]` |
| LinkTypeSchema | 없음 (RelationshipDto.properties만) | 정식 스키마: `linkType`, `direction`, `temporality`, constraints |
| 온톨로지 판별 기준 | 없음 | 4대 조건 명문화 |

---

## 2. 온톨로지 객체 판별 기준

"이것이 시스템의 온톨로지에 들어가야 하는가?"를 판단하는 체크리스트:

| 조건 | 질문 | 예시 (O) | 예시 (X) |
| --- | --- | --- | --- |
| **Identity** | 고유하게 식별 가능한가? | "3층 환경", "냉동기-A" | "습도라는 개념" (PropertyDefinition) |
| **Typed** | 타입으로 분류 가능한가? | `factory_floor` 타입 | 유일무이한 존재 |
| **Linkable** | 다른 객체와 관계를 맺는가? | "냉동기가 3층에 위치" | "cm 단위" (단위 정의) |
| **Observable** | 시간에 따라 상태가 변하는가? | "3층 온도 25°C" | "지구 중력 9.8" (물리 상수) |

4개 중 3개 이상 만족하면 Object, 아니면 Object의 Property이거나 시스템 메타데이터다.

---

## 3. 닫힌 체계 (Closed Enums)

시스템 코드가 의존하는 값. 변경 시 코드 수정 필요. 최소한으로 유지한다.

### 3.1 DataType

속성 값의 기본 자료형. 직렬화/역직렬화/검증의 근간.

```
number | string | boolean | datetime | array | object
```

### 3.2 SimulationBehavior

속성이 시뮬레이션에서 어떻게 변화하는지. 각 값에 대응하는 `IPropertySimulator` 구현체가 필요 (Phase 12).

```
constant | settable | rate | accumulator | derived
```

| 값 | 설명 | tick 계산 | 예시 (도메인 무관) |
| --- | --- | --- | --- |
| `constant` | 절대 변하지 않음 | 없음 (패치 거부) | 무게, 정격 용량 |
| `settable` | 외부 이벤트로만 변경 | 없음 (패치 수용) | 목표 온도, 설정 속도 |
| `rate` | 매 tick 변화량 적용 | `value += delta * dt` | 속도, 전력 소비량, 체온 변화 |
| `accumulator` | 초기량에서 증감 (min/max) | `stored += inflow - outflow` | 배터리 잔량, 탱크 수위, 재고 |
| `derived` | 다른 속성으로부터 계산 | 의존 속성 변경 시 재계산 | 효율, 사용률, 가동률 |

새 behavior 추가 시 대응 `IPropertySimulator` 구현 필수 (반닫힌).

### 3.3 Mutability

```
immutable | mutable
```

### 3.4 Traits — 객체 행동 특성 3축

도메인 용어 대신 존재론적 행동 특성으로 분류. 모든 도메인에서 동일하게 적용된다.

**Persistence** — 존재 지속성

```
permanent | durable | transient
```

| 값 | 의미 | 예시 |
| --- | --- | --- |
| `permanent` | 시스템 수명과 함께 존재 | 건물, 지역, 물리 법칙 |
| `durable` | 장기 존재, 생성/폐기 가능 | 설비, 환자, 차량, 기지국 |
| `transient` | 일시적, 생성 후 소멸 | 배송 건, 진료 세션, 시뮬레이션 Run |

**Dynamism** — 상태 변화성

```
static | dynamic | reactive
```

| 값 | 의미 | 예시 |
| --- | --- | --- |
| `static` | 생성 후 속성 변하지 않음 | KPI 목표, 정책 정의 |
| `dynamic` | 외부 이벤트나 시간에 의해 변함 | 설비 상태, 환자 바이탈, 재고 |
| `reactive` | 다른 객체의 변화에 반응하여 변함 | 파생 지표, 알림 조건, 집계값 |

**Cardinality** — 인스턴스 성격

```
singular | enumerable | streaming
```

| 값 | 의미 | 예시 |
| --- | --- | --- |
| `singular` | 타입당 하나 또는 소수 | 공장 환경, 시스템 설정 |
| `enumerable` | 식별 가능한 개별 인스턴스 | 설비, 환자, 차량 |
| `streaming` | 연속적으로 생성되는 이벤트성 | 센서 판독, 거래 기록 |

### 3.5 Link 특성

**LinkDirection**

```
directed | bidirectional | hierarchical
```

**LinkTemporality**

```
permanent | durable | event_driven
```

---

## 4. 열린 체계 (Open Schemas)

데이터로 관리. 코드 변경 없이 확장 가능.

### 4.1 PropertyDefinition

```json
{
  "key": "storedEnergy",
  "dataType": "number",
  "unit": "Wh",
  "simulationBehavior": "accumulator",
  "mutability": "mutable",
  "baseValue": 5000,
  "constraints": { "min": 0, "max": 10000 },
  "required": true
}
```

| 필드 | 체계 | 근거 |
| --- | --- | --- |
| `key` | 열린 | 도메인이 자유 정의 |
| `dataType` | 닫힌 | 타입 시스템의 근간 |
| `unit` | 열린 | "kg", "Wh", "patients/hr" 등 도메인마다 다름 |
| `simulationBehavior` | 반닫힌 | 엔진 코드 의존, 확장 가능하나 구현 필요 |
| `mutability` | 닫힌 | 2개 값으로 충분 |
| `baseValue` | 열린 | 타입에 따른 임의 값 |
| `constraints` | 열린 | min, max, pattern, enum 등 |
| `required` | 닫힌 | boolean |

### 4.2 ObjectTypeSchema

```json
{
  "objectType": "freezer",
  "displayName": "냉동기",
  "traits": {
    "persistence": "durable",
    "dynamism": "dynamic",
    "cardinality": "enumerable"
  },
  "classifications": [
    { "taxonomy": "industry", "value": "manufacturing.equipment" },
    { "taxonomy": "functional", "value": "consumer" }
  ],
  "properties": [ /* PropertyDefinition[] */ ],
  "allowedLinks": [
    {
      "linkType": "supplies",
      "direction": "inbound",
      "targetTraits": { "dynamism": "dynamic" }
    }
  ]
}
```

**traits** — 시스템이 읽는 행동 규칙 (닫힌 체계). 시뮬레이션 엔진, 관계 검증, GC 정책 등이 이 값으로 분기한다.

**classifications** — 사람이 읽는 의미 태그 (열린 체계). 검색, 필터링, UI 그룹핑, 권한 관리 등에 사용.

```
traits는 컴퓨터가 읽고,
classifications는 사람이 읽는다.
```

### 4.3 LinkTypeSchema

```json
{
  "linkType": "supplies",
  "displayName": "공급",
  "direction": "directed",
  "temporality": "durable",
  "fromConstraint": {
    "requiredTraits": { "dynamism": "dynamic" }
  },
  "toConstraint": {
    "requiredTraits": { "dynamism": "dynamic" }
  },
  "properties": [
    { "key": "ratio", "dataType": "number", "baseValue": 1.0, "constraints": { "min": 0, "max": 1 } },
    { "key": "transfers", "dataType": "array", "baseValue": [] }
  ]
}
```

### 4.4 Classification

```json
{ "taxonomy": "industry", "value": "manufacturing.equipment" }
```

`taxonomy`는 열린 문자열. 권장 네이밍:
- `industry` — 산업 도메인 (`manufacturing.equipment`, `healthcare.device`)
- `functional` — 시스템 내 역할 (`producer`, `consumer`, `monitor`)
- `regulatory` — 규제 관련 (`hipaa.phi`, `iso27001.asset`)
- `custom` — 고객 자유 정의

---

## 5. 도메인 무관 검증 — traits 조합 예시

| 도메인 | 객체 예시 | traits 조합 | 이전 표현 (도메인 어휘) |
| --- | --- | --- | --- |
| 제조 | 냉동기 | durable + dynamic + enumerable | "asset" |
| 의료 | 환자 | durable + dynamic + enumerable | "asset"에 안 맞음 |
| 의료 | 진료 세션 | transient + dynamic + enumerable | "flow"에 억지로 매핑 |
| 물류 | 창고 | permanent + dynamic + singular | "environment"에 애매함 |
| 발전 | KPI 목표 | durable + static + enumerable | "constraint" |
| 통신 | 트래픽 측정 | transient + reactive + streaming | 카테고리 없음 |

모든 예시가 traits 3축만으로 자연스럽게 분류된다.

---

## 6. 거버넌스 규칙

1. 모든 ObjectTypeSchema는 `traits` 3개 축을 반드시 정의한다.
2. PropertyDefinition의 `key`는 objectType 내에서 고유하다.
3. `required: true`인 속성은 인스턴스 생성 시 baseValue 또는 명시적 값이 필수다.
4. `mutability: immutable` 속성은 생성 후 패치 불가다.
5. LinkTypeSchema의 constraint를 만족하지 않는 관계는 생성 불가다 (MVP에서는 경고, 확장 시 거부).
6. `classifications`의 taxonomy에 시스템 예약어를 사용하지 않는다.
7. 새 SimulationBehavior 추가 시 대응 `IPropertySimulator` 구현이 필수다.

---

## 7. 산출물

> **계층 분리 노트**: 이 Phase의 산출물은 모두 `shared/ontology-schemas/`에 위치합니다. 런타임 HTTP API 계약(`shared/api-schemas/openapi.json`)과 **의도적으로 분리**되어 있으며, Phase 11에서 `/api/object-type-schemas` 엔드포인트가 추가될 때 두 계층이 연결됩니다.

| 산출물 | 경로 | 내용 |
| --- | --- | --- |
| 닫힌 체계 enum 공통 정의 | `shared/ontology-schemas/ontology-common-defs.json` | JSON Schema `$defs` |
| PropertyDefinition 스키마 | `shared/ontology-schemas/property-definition.json` | JSON Schema |
| ObjectTypeSchema 스키마 | `shared/ontology-schemas/object-type-schema.json` | JSON Schema |
| LinkTypeSchema 스키마 | `shared/ontology-schemas/link-type-schema.json` | JSON Schema |
| 예시 ObjectType: freezer | `shared/ontology-schemas/examples/freezer.json` | 냉동기 타입 정의 |
| 예시 ObjectType: battery | `shared/ontology-schemas/examples/battery.json` | 배터리 타입 정의 |
| 예시 ObjectType: conveyor | `shared/ontology-schemas/examples/conveyor.json` | 컨베이어 타입 정의 |
| 설계 문서 | `documentation/backend/ontology-metamodel.md` | 본 문서의 상세화 |

---

## 8. 완료 기준

- [x] 닫힌 체계의 모든 enum 값이 문서에 확정됨
- [x] ObjectTypeSchema, LinkTypeSchema, PropertyDefinition의 JSON Schema가 `shared/ontology-schemas/`에 존재
- [x] 3개 예시 ObjectType이 정의되고 JSON Schema 유효성 검증 통과 (`validate_phase10_examples.py`)
- [x] 온톨로지 판별 기준 4대 조건이 문서화됨
- [x] 거버넌스 규칙 7항이 문서화됨
- [x] `schemaVersion` 필드 포함 (G-5 이행)
- [x] `shared/ontology-schemas/`와 `shared/api-schemas/` 계층 분리 완료
- [x] 런타임 서비스 코드 변경 없음 (설계/스키마/문서 산출물만 반영)

---

## 9. 참고

- [00-overview.md](00-overview.md)
- [as-is Phase 10.0](../as-is-phases/2026-03-27-development-plan.md) — 섹션 4
