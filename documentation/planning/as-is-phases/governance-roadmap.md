# 거버넌스 프로젝트 연결 로드맵

본 문서는 [2026-03-25-development-plan.md](2026-03-25-development-plan.md)에서 분리한 **계약 준수 검증**과 **거버넌스 프로젝트 연결 준비** 계획입니다. 속성 타입 시스템(Phase 10, [2026-03-27-development-plan.md](2026-03-27-development-plan.md)) 완료 후 진행합니다.

---

## 1. 배경

Phase 8에서 `shared/`를 Single Source of Truth로 전환하고, 이벤트 엔벨로프·스키마·토픽·API 계약을 정합성 있게 정리했다. 그러나 아직 **어떤 서비스도 `shared/*.json`으로 런타임/테스트 검증을 하지 않는다**. 거버넌스 프로젝트가 "계약 위반을 탐지"하려면, 각 서비스에 검증 포인트가 심어져 있어야 한다.

### 선행 조건

- Phase 8 (스키마 정합성 확보) — 완료
- Phase 9 (애플리케이션 완성도 보강) — 완료
- Phase 10 (속성 타입 시스템) — 진행 예정

Phase 10에서 State 스키마가 동적 key-value로 변경되므로, 검증 스키마도 이에 맞춰 업데이트해야 한다. 따라서 Phase 10 완료 후 진행하는 것이 효율적이다.

---

## 2. 계획 범위

| Phase | 제목 | 우선순위 |
| --- | --- | --- |
| G-1 | Pipeline: Kafka 메시지 스키마 검증 | P0 |
| G-2 | Backend: 발행 메시지 스키마 검증 (테스트) | P0 |
| G-3 | Golden File 테스트 세트 | P1 |
| G-4 | Frontend: API 응답 검증 (개발 모드) | P2 |
| G-5 | 스키마 버전 라이프사이클 정의 | P1 |
| G-6 | 호환성 체크 스크립트 | P1 |
| G-7 | CI 파이프라인 (GitHub Actions) | P1 |

---

## 3. 계약 준수 검증

### Phase G-1 — Pipeline: Kafka 메시지 스키마 검증

- **목표**: Pipeline이 Kafka 메시지를 소비할 때, `shared/event-schemas` 스키마로 실제 검증. 검증 실패 메시지를 격리(DLQ 패턴).
- **계획**:
  - `jsonschema` 패키지 추가 (`pyproject.toml`)
  - `shared/event-schemas/schemas/` 경로에서 스키마 로딩하는 유틸리티 (`messaging/validation/schema_validator.py`)
  - `asset_worker.py` — 이벤트 디스패치 전 `validate(instance=event, schema=loaded_schema)` 호출
  - 검증 실패 시: 로그(WARNING) + MongoDB `dead_letter_events` 컬렉션에 원본 저장 + 처리 건너뜀
  - 테스트: 유효 이벤트 통과, 무효 이벤트(필수 필드 누락 등) 시 DLQ 저장 + 처리 미호출 검증
- **완료 기준**: 스키마 위반 메시지가 DLQ에 격리되고, 정상 메시지만 처리됨.
- **산출물**: `schema_validator.py`, `asset_worker.py` 수정, `dead_letter_events` 컬렉션 정의, 테스트.

---

### Phase G-2 — Backend: 발행 메시지 스키마 검증 (테스트)

- **목표**: 백엔드가 Kafka로 발행하는 메시지가 `shared/event-schemas` 스키마에 부합하는지 테스트 시점에 자동 검증.
- **계획**:
  - 테스트 프로젝트에 `NJsonSchema` 또는 `JsonSchema.Net` 패키지 추가
  - `SchemaValidationHelper` 유틸리티 — `shared/event-schemas/schemas/*.json` 로딩, `Validate(jsonString, schemaPath)` 메서드
  - 시뮬레이션 핸들러 테스트에서: mock `IEventPublisher`가 캡처한 `EventDto`를 JSON 직렬화 → `SchemaValidationHelper`로 검증 어설션 추가
  - 기존 테스트 통과 유지하며 스키마 검증 어설션만 추가
- **완료 기준**: 시뮬레이션·알람 관련 테스트가 스키마 검증을 포함하고, 스키마 불일치 시 테스트 실패.
- **산출물**: `SchemaValidationHelper`, 기존 테스트 파일 수정, NuGet 패키지 추가.

---

### Phase G-3 — Golden File 테스트 세트

- **목표**: 각 이벤트 타입별 유효/무효 예시 페이로드를 공유 fixtures로 관리. 모든 서비스의 테스트가 동일 fixtures를 사용.
- **계획**:
  - `shared/event-schemas/fixtures/` 디렉토리 구조:
    ```
    fixtures/
    ├── valid/
    │   ├── simulation.state.updated.node.json
    │   ├── simulation.state.updated.propagation.json
    │   ├── alert.generated.warning.json
    │   ├── alert.generated.error.json
    │   ├── asset.created.json
    │   └── asset.health.updated.json
    └── invalid/
        ├── missing-event-type.json
        ├── missing-asset-id.json
        ├── wrong-severity-enum.json
        ├── extra-unknown-field.json
        └── missing-schema-version.json
    ```
  - Pipeline 테스트: fixtures 로딩 → `schema_validator` 통과/실패 검증
  - Backend 테스트: fixtures 로딩 → `SchemaValidationHelper` 통과/실패 검증
- **완료 기준**: `valid/` fixtures 전체가 스키마 검증 통과, `invalid/` fixtures 전체가 스키마 검증 실패. 양쪽 서비스 테스트에서 동일 결과.
- **산출물**: `fixtures/` 디렉토리, Pipeline·Backend 테스트 수정.

---

### Phase G-4 — Frontend: API 응답 검증 (개발 모드)

- **목표**: 개발 환경에서 API 응답이 OpenAPI 스키마에 부합하는지 자동 경고.
- **계획**:
  - `shared/api/` 또는 `shared/lib/`에 `responseValidator.ts` — `openapi.json`의 `components/schemas` 기반 zod 스키마 또는 ajv 검증
  - `httpClient.ts` 인터셉터에 개발 모드 분기: `import.meta.env.DEV`일 때만 응답 검증, 실패 시 `console.warn`
  - 프로덕션 빌드에서는 제거 (tree-shaking 또는 조건부 import)
- **완료 기준**: 개발 모드에서 스키마 불일치 API 응답 시 콘솔 경고 출력. 프로덕션 번들에 검증 코드 미포함.
- **산출물**: `responseValidator.ts`, `httpClient.ts` 수정.

---

## 4. 거버넌스 프로젝트 연결 준비

이 단계를 완료하면, 후속 데이터 거버넌스 프로젝트가 Factory MES를 대상 서비스로 즉시 활용할 수 있다.

### Phase G-5 — 스키마 버전 라이프사이클 정의

- **목표**: 스키마 변경의 호환성 규칙과 버전 관리 프로세스를 명시.
- **계획**:
  - `shared/event-schemas/VERSIONING.md` 신규 생성:
    - **하위 호환(backward compatible)** 변경: optional 필드 추가, enum 값 추가 → 같은 메이저 버전 내 허용
    - **하위 비호환(breaking)** 변경: required 필드 추가, 필드 삭제, 타입 변경 → 새 메이저 버전 필수
    - 최소 1개 이전 버전 동시 지원 기간 (sunset policy)
    - 버전 전환 프로세스: draft → active → deprecated → removed
    - **소비자 버전 라우팅 전략**: 소비자는 메시지의 `schemaVersion` 값을 먼저 읽어 해당 버전의 스키마 파일(`versions/vN.json` 매니페스트)로 라우팅한 후 페이로드 검증 수행. 엔벨로프 `schemaVersion`이 `pattern: "^v\\d+$"`으로 정의되어 있으므로(Phase 8.1), 라우팅 전 엔벨로프 검증은 모든 버전에서 통과함.
    - **`context` 필드 정책**: `context` 안의 운영 메타데이터는 버전 관리 대상 외. 공식 계약 변경은 `payload` 변경만 해당.
  - `shared/event-schemas/versions/v2.json` 예시 작성 (v1에서 하나의 호환 변경을 적용한 사례)
- **완료 기준**: 버전 관리 규칙이 문서화되고, 소비자 라우팅 전략이 명시되며, v1 → v2 전환 사례가 존재.
- **산출물**: `VERSIONING.md`, `v2.json`.

---

### Phase G-6 — 호환성 체크 스크립트

- **목표**: 두 버전의 스키마를 비교하여 breaking change를 자동 탐지하는 스크립트. 거버넌스 프로젝트의 핵심 기능 프로토타입.
- **계획**:
  - `shared/scripts/check-compatibility.py`:
    - 두 스키마 파일(v_old, v_new)을 입력받아 비교
    - 탐지 규칙: required 필드 추가 → breaking, 필드 삭제 → breaking, enum 값 축소 → breaking, type 변경 → breaking, optional 필드 추가 → compatible
    - 출력: `COMPATIBLE` / `BREAKING` + 변경 상세 목록
  - CI에서 `shared/event-schemas/versions/` 변경 시 자동 실행
- **완료 기준**: v1 → v2 비교 시 호환/비호환 판정이 정확하게 출력됨.
- **산출물**: `check-compatibility.py`, 테스트.

---

### Phase G-7 — CI 파이프라인 (GitHub Actions)

- **목표**: 스키마 검증, 테스트, 호환성 체크를 자동화하는 CI 구성.
- **계획**:
  - `.github/workflows/ci.yml`:
    - **schema-validation**: `shared/event-schemas/fixtures/`의 golden files를 jsonschema로 검증
    - **schema-compatibility**: PR에서 `shared/event-schemas/versions/` 변경 시 `check-compatibility.py` 실행
    - **backend-tests**: `dotnet test`
    - **pipeline-tests**: `pytest`
    - **frontend-tests**: `pnpm test`
  - PR 머지 조건: schema-validation + 각 서비스 테스트 통과
- **완료 기준**: PR 생성 시 5개 job이 자동 실행되고, 실패 시 머지 차단.
- **산출물**: `.github/workflows/ci.yml`.

---

## 5. 거버넌스 프로젝트 인터페이스

위 Phase 완료 후, 거버넌스 프로젝트가 Factory MES에서 읽을 수 있는 것:

| 거버넌스 기능 | Factory MES 제공물 | Phase |
| --- | --- | --- |
| 스키마 레지스트리 | `shared/event-schemas/versions/*.json` + `shared/api-schemas/openapi.json` | 8.1~8.5 (완료) |
| 계약 범위 정의 | `CONTRACT.md` (public/internal 분류) + `FIELD-MAPPING.md` | 8.3, 8.6 (완료) |
| 운영 메타데이터 확장 | 엔벨로프 `context` 필드 (traceId, correlationId 등 자유 확장) | 8.1 (완료) |
| 다중 버전 공존 | `schemaVersion` pattern 기반 소비자 라우팅, `VERSIONING.md` 전략 | 8.1 (완료), G-5 |
| 계약 위반 탐지 | Pipeline DLQ (`dead_letter_events`), Backend 테스트 스키마 검증 | G-1, G-2 |
| 호환성 분석 | `check-compatibility.py` 출력, `VERSIONING.md` 규칙 | G-5, G-6 |
| 서비스 간 데이터 흐름 | `topics.json` (토픽·이벤트 매핑) + `FIELD-MAPPING.md` | 8.4, 8.6 (완료) |
| 변경 이력 | `CHANGELOG.md` + git diff on `shared/` | G-5 |
| 자동 검증 | GitHub Actions CI (`ci.yml`) | G-7 |

---

## 6. 참고 문서

- [2026-03-25-development-plan.md](2026-03-25-development-plan.md) — Phase 8~9 계획 및 완료 기록
- [2026-03-27-development-plan.md](2026-03-27-development-plan.md) — Phase 10: 속성 타입 시스템
- [event-types.md](../shared/event-types.md) — 이벤트 타입 Command/Observation 분류
- [simulation-engine-architecture.md](../backend/simulation-engine-architecture.md) — Simulation 모듈 구조

---
