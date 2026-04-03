# Phase 10 Sign-off and Phase 11 Handoff

## Sign-off

### Completed outputs

| 파일 | 비고 |
| --- | --- |
| `shared/ontology-schemas/ontology-common-defs.json` | 닫힌 체계 enum 단일 소스 |
| `shared/ontology-schemas/property-definition.json` | PropertyDefinition JSON Schema |
| `shared/ontology-schemas/object-type-schema.json` | ObjectTypeSchema JSON Schema (schemaVersion 포함) |
| `shared/ontology-schemas/link-type-schema.json` | LinkTypeSchema JSON Schema (schemaVersion 포함) |
| `shared/ontology-schemas/examples/freezer.json` | |
| `shared/ontology-schemas/examples/battery.json` | |
| `shared/ontology-schemas/examples/conveyor.json` | |
| `shared/ontology-schemas/validate_phase10_examples.py` | |
| `documentation/backend/ontology-metamodel.md` | |
| `documentation/planning/to-be-phases/phase-10-baseline-checklist.md` | |

### 계층 분리 결과

- `shared/ontology-schemas/` — Layer 1 메타계층 계약 (이 Phase 산출물)
- `shared/api-schemas/openapi.json` — 런타임 HTTP API 계약 (Phase 1~9 산출물, Phase 11에서 확장)
- 두 계층의 연결은 Phase 11에서 이루어짐

### Validation evidence

- Command: `python3 shared/ontology-schemas/validate_phase10_examples.py`
- Result:
  - valid fixtures: pass (3/3)
  - negative fixtures: rejected as expected (2/2)
- G-5 이행: `schemaVersion` 필드 추가 (`^v\d+$` 패턴, 예: `v1`)

## Phase 11 Handoff Inputs

### 1) Instance creation validation
- At object instance create API, load matching `ObjectTypeSchema` from `shared/ontology-schemas/object-type-schema.json`.
- Enforce required `traits` presence at schema level and required property value coverage at runtime.

### 2) Dynamic properties mapping
- Map object state property bag to `PropertyDefinition`:
  - key existence
  - type compatibility (`dataType`)
  - required presence (`required`)
  - patch rule gate (`mutability`)

### 3) Link validation pre-check
- Before creating relationship, check `LinkTypeSchema` direction/temporality contract.
- Apply `fromConstraint.requiredTraits` and `toConstraint.requiredTraits` against source/target object type traits.

### 4) openapi.json 연결 (G-4 이행)
- Phase 11에서 `/api/object-type-schemas` 엔드포인트를 `openapi.json`에 추가할 때, payload 계약은 `shared/ontology-schemas/object-type-schema.json`을 소스로 동기화해야 함.
- 동일하게 `/api/link-type-schemas`는 `shared/ontology-schemas/link-type-schema.json` 기준.

### 5) Simulator interface prep
- Keep `simulationBehavior` as contract in Phase 11 runtime path.
- Implement actual `IPropertySimulator` strategy dispatch in Phase 12.
