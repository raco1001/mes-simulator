# Ontology Schemas (Layer 1 Metamodel)

Phase 10에서 확정된 온톨로지 메타모델 계약 파일 모음입니다.

## 역할

이 디렉토리의 파일들은 **Layer 1 메타계층** 계약입니다.
런타임 HTTP API 계약(`shared/api-schemas/openapi.json`)과 **의도적으로 분리**되어 있습니다.

| 계층 | 위치 | 역할 |
| --- | --- | --- |
| Layer 1 메타계층 | `shared/ontology-schemas/` | ObjectType/LinkType 정의 규칙 |
| 런타임 HTTP 계약 | `shared/api-schemas/openapi.json` | 서버 ↔ 클라이언트 API payload |

Phase 11에서 `/api/object-type-schemas` 엔드포인트가 추가될 때, `openapi.json`의 payload 정의는 이 디렉토리의 스키마를 소스로 동기화합니다.

## 구조

```
ontology-schemas/
├── ontology-common-defs.json    # 닫힌 체계 enum 공통 정의 (단일 소스)
├── property-definition.json     # PropertyDefinition 스키마
├── object-type-schema.json      # ObjectTypeSchema 스키마
├── link-type-schema.json        # LinkTypeSchema 스키마
├── examples/                    # Golden fixtures (스키마 검증용)
│   ├── freezer.json
│   ├── battery.json
│   ├── conveyor.json
│   └── invalid/                 # 의도적 실패 케이스
│       ├── invalid-missing-required.json
│       └── invalid-enum.json
├── validate_phase10_examples.py # 검증 스크립트
└── README.md
```

## 닫힌 체계 Enums

`ontology-common-defs.json`에 단일 소스로 정의. 코드 변경 없이 확장 불가.

| Enum | 값 |
| --- | --- |
| DataType | `number`, `string`, `boolean`, `datetime`, `array`, `object` |
| SimulationBehavior | `constant`, `settable`, `rate`, `accumulator`, `derived` |
| Mutability | `immutable`, `mutable` |
| TraitPersistence | `permanent`, `durable`, `transient` |
| TraitDynamism | `static`, `dynamic`, `reactive` |
| TraitCardinality | `singular`, `enumerable`, `streaming` |
| LinkDirection | `directed`, `bidirectional`, `hierarchical` |
| LinkTemporality | `permanent`, `durable`, `event_driven` |

## 검증 실행

```bash
python3 shared/ontology-schemas/validate_phase10_examples.py
```

기대 결과: valid fixtures 3종 PASS, negative fixtures 2종 rejected.

## 거버넌스

- 닫힌 enum 값 추가 시 코드 변경 필수 (Phase 12 `IPropertySimulator` 구현 등)
- `ObjectTypeSchema`는 traits 3축을 반드시 정의
- `schemaVersion`은 `^v\d+$` 패턴 (예: `v1`)
- 신규 ObjectType 추가 시 `examples/`에 golden fixture 추가 권장
