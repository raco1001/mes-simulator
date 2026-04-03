# 개발 계획 (2026-03-25)

본 문서는 Factory MES 프로젝트의 **데이터 계약 정합성 확보**와 **애플리케이션 완성도 보강**을 위한 개발 계획을 정리한 것입니다. Phase 8 (스키마 정합성)과 Phase 9 (애플리케이션 완성도)를 모두 완료하였으며, 진행 중 발견한 구조적 인사이트를 기록합니다.

---

## 1. 배경

### 계획 시점 상태 진단

Phase 7c.1 + UI-1까지 완료된 시점에서, 시뮬레이션 → Kafka → Pipeline → Alert → REST API 경로가 end-to-end로 동작한다. 그러나 `shared/` 디렉토리의 스키마가 **문서 저장소** 수준에 머물러 있고, 실제 코드와의 괴리(drift)가 누적되어 있다.

| 문제 영역 | 현상 | 해결 Phase |
| --- | --- | --- |
| 스키마 누락 | `simulation.state.updated`(가장 많이 사용되는 이벤트)에 스키마 파일 없음 | 8.2 완료 |
| 엔벨로프 drift | 코드가 발행하는 `runId` 필드가 스키마에 미정의 | 8.1 완료 |
| 토픽 불일치 | `topics.json`에 3개 토픽 정의, 실제 사용 1개 | 8.4, 9.4 완료 |
| API 스키마 비표준 | JSON Schema + 커스텀 `paths` 혼합 — OpenAPI 3.x 아님 | 8.5 완료 |
| 검증 부재 | 어떤 서비스도 `shared/*.json`으로 런타임/테스트 검증을 하지 않음 | 별도 로드맵으로 이관 |
| UI 미연결 | Alert API가 존재하지만 프론트엔드에서 호출하지 않음 | 9.2 완료 |
| 저장소 휘발 | `InMemoryAlertStore` — 서버 재시작 시 알람 소실 | 9.3 완료 |
| Redis 미사용 | 인프라에 Redis 컨테이너가 있지만 코드에서 사용하지 않음 | 9.4 완료 |

### 목표

1. **`shared/`를 Single Source of Truth(SSoT)로 전환** — 스키마가 진실이고, 코드가 이를 참조·검증하는 구조
2. **데이터 흐름 end-to-end 완성** — Asset → Kafka → Pipeline → Alert → API → UI

### 제약

- 면접 전(D-5)까지 Phase 8, 9 완료 목표 — 스키마 정합성 + 애플리케이션 완성도
- 기존 동작하는 코드의 기능 유지 (breaking change 최소화)

---

## 2. 계획 범위 및 완료 현황

| Phase | 제목 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| 8.1 | 이벤트 엔벨로프 공통 스키마 정의 | P0 | 완료 |
| 8.2 | `simulation.state.updated` 스키마 추가 | P0 | 완료 |
| 8.3 | 공개(public) vs 내부(internal) 이벤트 분류 | P0 | 완료 |
| 8.4 | `topics.json` 현실 반영 | P0 | 완료 |
| 8.5 | API 스키마 OpenAPI 3.x 표준화 | P1 | 완료 |
| 8.6 | 필드명 매핑 문서 (Kafka ↔ REST) | P1 | 완료 |
| 9.1 | Alert SSE 스트림 — Backend (새 포트·채널·엔드포인트) | P0 | 완료 |
| 9.2 | Alert SSE 스트림 — Frontend (EventSource·토스트) | P0 | 완료 |
| 9.3 | `MongoAlertStore` 구현 (어댑터 교체) | P0 | 완료 |
| 9.4 | Alert 토픽 분리 (`factory.asset.alert`) + Redis 제거 | P1 | 완료 |

---

## 3. Phase 8 — 스키마 정합성 확보 ("문서 → 계약") [완료]

`shared/`가 실제 와이어 포맷과 100% 일치하게 만든다. 거버넌스 프로젝트가 이 디렉토리를 읽으면 서비스의 모든 외부 계약을 파악할 수 있는 상태를 목표로 한다.

### Phase 8.1 — 이벤트 엔벨로프 공통 스키마 정의 [완료]

- **목표**: 모든 이벤트가 공유하는 엔벨로프 구조를 단일 스키마로 정의. 각 이벤트 스키마가 이를 `$ref`로 참조하도록 구조화.
- **배경**: 현재 `asset.created.json`, `asset.health.updated.json`, `alert.generated.json`이 각각 `eventType`, `assetId`, `timestamp`, `payload`를 반복 정의한다. 코드에서 실제 발행하는 `runId` 필드는 어떤 스키마에도 없다.
- **계획**:
  - `shared/event-schemas/schemas/event-envelope.json` 신규 생성:
    - `required`: `eventType`, `assetId`, `timestamp`, `schemaVersion`, `payload`
    - `optional`: `runId` (시뮬레이션 컨텍스트)
    - `optional`: `context` — 운영 메타데이터 전용 확장 포인트 (`additionalProperties: true`)
    - 엔벨로프 최상위는 `additionalProperties: false` — 공식 필드 목록 엄격 제어
    - `schemaVersion`은 `const: "v1"` 대신 `pattern: "^v\\d+$"` — 다중 버전 공존 시 소비자 라우팅 지원
  - 기존 3개 이벤트 스키마를 엔벨로프 `$ref` 기반으로 재구성 (envelope `allOf` + payload 확장)
  - `KafkaEventPublisher.cs`의 발행 객체에 `schemaVersion = "v1"` 필드 추가
  - Pipeline `build_alert_event`에 `schemaVersion` 필드 추가
- **설계 근거**:
  - `context` 분리: `traceId`, `correlationId`, `sourceService` 같은 운영 필드를 추가할 때마다 버전 bump 없이 수용. 거버넌스 프로젝트가 공식 계약(`payload`)과 운영 메타데이터(`context`)를 별도 레이어로 관리 가능.
  - `schemaVersion` pattern 완화: 런타임 검증 도입 시 소비자가 `schemaVersion` 값을 읽어 적절한 스키마 파일로 라우팅해야 하는데, `const: "v1"` 이면 v2 메시지가 엔벨로프 검증 자체에서 차단되어 DLQ 폭발로 이어짐. `pattern`으로 완화하면 엔벨로프 검증 통과 후 소비자 코드에서 버전별 분기 처리.
- **완료 기준**: 모든 이벤트 스키마가 `event-envelope.json`을 참조하고, `runId`·`schemaVersion`·`context`가 공식 필드로 정의됨. `schemaVersion`이 `pattern` 방식으로 정의됨.
- **산출물**: `event-envelope.json`, 기존 3개 스키마 수정, `KafkaEventPublisher.cs`·`asset_pipeline.py` 수정.

---

### Phase 8.2 — `simulation.state.updated` 스키마 추가 [완료]

- **목표**: 가장 많이 사용되지만 스키마가 없는 `simulation.state.updated` 이벤트의 계약을 정의.
- **배경**: 백엔드에서 2가지 페이로드 변형이 존재한다.
  - **노드 업데이트**: `{ tick, depth, status, temperature, power }` — `RunSimulationCommandHandler`에서 발행
  - **관계 전파**: `{ tick, depth, relationshipType, fromAssetId, relationshipId? }` — `SuppliesRule`, `ContainsRule`, `ConnectedToRule`에서 발행
- **계획**:
  - `shared/event-schemas/schemas/simulation.state.updated.json` 신규 생성:
    - 엔벨로프 `$ref` + `eventType` const `simulation.state.updated`
    - `payload`를 `oneOf`로 분리: `NodeUpdatePayload` (tick, depth, status, temperature, power) / `PropagationPayload` (tick, depth, relationshipType, fromAssetId, relationshipId?)
    - 공통 필수 필드: `tick` (integer), `depth` (integer)
  - `v1.json` 매니페스트에 `simulation.state.updated` 경로 추가
- **완료 기준**: `simulation.state.updated`의 두 변형이 스키마로 정의되고, 매니페스트에 등록됨. 기존 코드의 발행 페이로드가 스키마에 부합.
- **산출물**: `simulation.state.updated.json`, `v1.json` 수정.

---

### Phase 8.3 — 공개(public) vs 내부(internal) 이벤트 분류 [완료]

- **목표**: 서비스 경계를 넘는 이벤트(public)와 서비스 내부 이벤트(internal)를 명시적으로 구분. 거버넌스 프로젝트가 관리하는 대상 범위를 명확히 함.
- **배경**: `EventTypes.cs`에 7개 이상의 이벤트 타입이 정의되어 있지만, 실제로 Kafka를 통해 서비스 간 전달되는 것은 일부뿐이다.
- **계획**:
  - `shared/event-schemas/CONTRACT.md` 신규 생성 — 공개/내부 분류표:

    | 이벤트 | 분류 | 근거 |
    | --- | --- | --- |
    | `simulation.state.updated` | **public** | Pipeline이 소비 |
    | `alert.generated` | **public** | Backend가 재소비 |
    | `asset.created` | **public** | Pipeline 핸들러 존재 (발행 미구현) |
    | `asset.health.updated` | **public** | Pipeline 핸들러 존재 (발행 미구현) |
    | `power_changed` | **internal** | Backend 시뮬레이션 내부 |
    | `state_transitioned` | **internal** | Backend 시뮬레이션 내부 |
    | `start_machine`, `stop_machine`, `change_speed` | **internal** (command) | 시뮬레이션 제어, Live 전환 시 어댑터 교체 대상 |

  - `shared/event-schemas/` 디렉토리에는 **public 이벤트만** 스키마 배치
  - `documentation/shared/event-types.md`에 분류 기준과 `CONTRACT.md` 참조 추가
- **완료 기준**: public/internal 분류가 문서화되고, `shared/event-schemas/schemas/` 에 public 이벤트 스키마만 존재.
- **산출물**: `CONTRACT.md`, `event-types.md` 수정.

---

### Phase 8.4 — `topics.json` 현실 반영 [완료]

- **목표**: 토픽 정의 파일을 실제 운영 상태와 일치시킴.
- **배경**: `topics.json`에 3개 토픽(`factory.asset.events`, `factory.asset.health`, `factory.asset.alert`)이 정의되어 있지만, 코드에서는 `factory.asset.events` 하나만 사용한다. 이벤트 목록에도 `simulation.state.updated`가 누락.
- **계획**:
  - `factory.asset.events`의 `events` 배열에 `simulation.state.updated` 추가
  - 미사용 토픽 2개에 `"status": "planned"` 필드 추가하여 현재 미활성 상태 명시
  - `topics.json` 스키마에 `status` 필드(`"active"` | `"planned"` | `"deprecated"`) 정의 추가
- **완료 기준**: `topics.json`의 active 토픽과 이벤트 목록이 실제 코드 사용과 일치.
- **산출물**: `topics.json` 수정.

---

### Phase 8.5 — API 스키마 OpenAPI 3.x 표준화 [완료]

- **목표**: `shared/api-schemas/`의 REST API 계약을 표준 OpenAPI 3.0 형식으로 통합. 거버넌스 도구가 파싱 가능한 형태로 전환.
- **배경**: 현재 `assets.json`, `state.json`, `relationships.json`이 JSON Schema `definitions` + 커스텀 `paths` 혼합 포맷이다. OpenAPI도 아니고 순수 JSON Schema도 아닌 비표준 형태.
- **계획**:
  - `shared/api-schemas/openapi.json` 단일 파일로 통합:
    - OpenAPI 3.0.3 형식
    - `components/schemas/`에 `AssetDto`, `StateDto`, `RelationshipDto`, `CreateAssetRequest`, `UpdateAssetRequest`, `CreateRelationshipRequest`, `UpdateRelationshipRequest`, `AlertDto` 정의
    - `paths/`에 전체 API 엔드포인트 정의 (`/api/assets`, `/api/states`, `/api/relationships`, `/api/alerts`, `/api/simulation/*`)
  - 기존 `assets.json`, `state.json`, `relationships.json`은 유지하되, `README.md`에 "레거시, `openapi.json` 참조" 안내
  - 백엔드 Swagger 출력과 `openapi.json`의 일치 여부를 검증할 수 있는 기반 확보
- **완료 기준**: `openapi.json`이 OpenAPI 3.0.3 유효성 검증을 통과하고, 전체 REST API 계약을 포함.
- **산출물**: `openapi.json` 신규, `README.md` 수정.

---

### Phase 8.6 — 필드명 매핑 문서 (Kafka ↔ REST) [완료]

- **목표**: Kafka 이벤트와 REST API 간의 의도적 필드명 차이를 문서화.
- **배경**: 같은 이벤트가 Kafka에서는 `timestamp` / `runId`, REST API에서는 `occurredAt` / `simulationRunId`로 표현된다. 의도적 차이이지만 문서화되지 않아 계약 불일치로 오해할 수 있다.
- **계획**:
  - `shared/FIELD-MAPPING.md` 신규 생성:

    | 개념 | Kafka 필드명 | REST API 필드명 | 설명 |
    | --- | --- | --- | --- |
    | 이벤트 발생 시각 | `timestamp` | `occurredAt` | Kafka: 이벤트 시점, REST: DB 저장 시점 맥락 |
    | 시뮬레이션 실행 ID | `runId` | `simulationRunId` | Kafka: 간결한 키, REST: 도메인 명시적 이름 |
    | 이벤트 타입 | `eventType` | `eventType` | 동일 |
    | 대상 에셋 | `assetId` | `assetId` | 동일 |

  - 각 매핑에 "왜 다른지"의 설계 근거 포함
- **완료 기준**: Kafka와 REST 간 필드명 차이가 문서화되고, 설계 근거가 명시됨.
- **산출물**: `FIELD-MAPPING.md`.

---

## 4. Phase 9 — 애플리케이션 완성도 보강 [완료]

데이터 흐름의 끝단(UI)과 저장소 내구성을 보강하여, 전체 파이프라인이 실제로 동작하게 만든다.

### Phase 9.1 — Alert SSE 스트림 Backend [완료]

- **목표**: Kafka에서 소비한 Alert를 REST Polling 없이 연결된 프론트엔드에 즉시 Push. `IAlertNotifier` 포트를 추가하여 Hexagonal 구조 유지.
- **배경**: `KafkaAlertConsumerService`가 Alert를 `IAlertStore`에만 저장하고 끝난다. 프론트엔드가 이를 알려면 Polling이 필요한데, Slack 알림과 같은 즉시 Push 경험을 위해 SSE(Server-Sent Events)를 선택한다. SSE는 단방향(서버→클라이언트) 표준 HTTP 스트림으로, Alert 수신이라는 목적에 정확히 맞고 별도 라이브러리가 필요 없다.
- **아키텍처**:
  ```
  KafkaAlertConsumerService
    ├─ IAlertStore.Add(alert)        → 이력 저장 (기존)
    └─ IAlertNotifier.Notify(alert)  → SSE Push (신규)
                                          ↓
                                   SseAlertChannel
                                   (Channel<AlertDto> per connection)
                                          ↓
                           AlertController.StreamAlerts()
                           GET /api/alerts/stream  →  EventSource (Frontend)
  ```
- **산출물**: `IAlertNotifier.cs`, `SseAlertChannel.cs`, `AlertController.cs` 수정, `KafkaAlertConsumerService.cs` 수정, `Program.cs` 수정, 테스트.

---

### Phase 9.2 — Alert SSE 스트림 Frontend [완료]

- **목표**: 백엔드 SSE 스트림을 구독하여 Alert 발생 시 Slack 알림처럼 즉시 토스트 표시. Polling 없이 실시간 반응.
- **산출물**: `entities/alert/` (types, alertStream, alertApi), `AlertToast` 컴포넌트, 앱 레벨 구독 연결, 테스트.

---

### Phase 9.3 — `MongoAlertStore` 구현 (어댑터 교체) [완료]

- **목표**: `InMemoryAlertStore`를 `MongoAlertStore`로 교체하여 Alert 내구성 확보. Hexagonal 아키텍처의 "어댑터 교체" 가치를 코드로 증명.
- **산출물**: `MongoAlertStore.cs`, `MongoAlertDocument.cs`, `MODEL.md` 수정, `Program.cs` 수정, 테스트.

---

### Phase 9.4 — Alert 토픽 분리 + Redis 제거 [완료]

- **목표**: Alert 이벤트를 전용 Kafka 토픽으로 분리하여 토픽 설계를 정리하고, 사용하지 않는 Redis를 인프라에서 제거.
- **구현 결과**:
  - Alert 이벤트가 `factory.asset.alert` 전용 토픽으로 발행·소비됨
  - `factory.asset.events`에는 시뮬레이션 이벤트만 흐름
  - Redis 서비스가 Compose에서 제거됨
  - Docker Compose 파일 구조 변경: 기존 `docker-compose.infra.yml`/`docker-compose.app.yml`/`docker-compose.full.yml` → `docker/infra/docker-compose.yml` + `docker/app/docker-compose.yml`로 재편
- **설계 근거**:
  - 토픽 분리: 소비자가 관심 없는 메시지를 읽고 버리는 불필요한 처리 제거. 토픽 = 이벤트 계약이라는 Kafka 표준 패턴 준수.
  - Redis 제거: 현재 규모에서 명확한 용도 없이 인프라 복잡도만 높이는 컴포넌트 제거. 확장 시 `IAlertNotifier` 구현체를 `SseAlertChannel`(메모리) → `RedisBackedSseChannel`(Redis Pub/Sub)으로 교체하면 SSE 실시간성 유지 가능.

---

## 5. 회고 및 인사이트

Phase 8-9를 완료하고 리팩토링을 진행하면서, 시뮬레이션 엔진의 **구조적 한계**가 드러났다.

### 5.1 하드코딩된 상태 필드

`StateDto`와 `StatePatchDto`가 `CurrentTemp`, `CurrentPower`를 고정 필드로 가지고 있다. 모든 에셋이 동일한 구조의 상태를 강제받는 셈이다.

```csharp
// StatePatchDto.cs — 현재
public sealed record StatePatchDto
{
    public double? CurrentTemp { get; init; }
    public double? CurrentPower { get; init; }
    public string? Status { get; init; }
    public string? LastEventType { get; init; }
}
```

이 구조는 "에셋 = 공장 자원의 일반화"라는 프로젝트 목표(Phase 1)와 배치된다. freezer는 온도가 중요하고, 배터리는 저장 전력이 중요하고, 컨베이어는 속도가 중요한데, 모두가 `CurrentTemp`와 `CurrentPower`만 가진다.

### 5.2 물리량 성격에 무관한 시뮬레이션

시뮬레이션 엔진이 모든 속성을 동일하게 처리한다. 그러나 실제 물리량은 성격에 따라 시뮬레이션 방법이 달라야 한다:

| 물리량 예시 | 시뮬레이션 성격 | 현재 엔진의 처리 |
| --- | --- | --- |
| 무게 | 고정값, 절대 변하지 않음 | 구분 없음 |
| 부피 | 에셋에 따라 고정 또는 가변 | 구분 없음 |
| 속도 | 매 tick 변화량(delta) 적용 | 구분 없음 |
| 저장 전력 | 초기값에서 소비량만큼 감소 (accumulator) | 구분 없음 |
| 전력 소비량 | 매 tick 변화량 | 구분 없음 |

### 5.3 다음 단계 방향

이 인사이트를 바탕으로 Phase 10에서는 **속성 타입 시스템**을 도입한다. 핵심은:

1. **State를 동적 Key-Value로 전환** — 하드코딩 필드 제거, 에셋별 자유 속성
2. **PropertyDefinition 도입** — 에셋 타입별 속성 스키마 (key, dataType, mutability, simulationBehavior, baseValue)
3. **SimulationBehavior별 엔진 전략** — 속성의 물리적 성격에 따라 tick 계산 방법 분리
4. **관계 기반 속성 흐름 정교화** — 어떤 속성을 어떤 비율로 전달할지 관계 단위 제어

상세 계획은 [2026-03-27-development-plan.md](2026-03-27-development-plan.md)에 기록한다.

---

## 6. 면접 대비 요약

Phase 8 + 9 완료로 면접에서 꺼낼 수 있는 이야기:

| JD 키워드 | Factory MES 증명 | 스키마 거버넌스 보강 |
| --- | --- | --- |
| **Real-time** | SimulationEngine → Kafka → Pipeline → **SSE Push** → 브라우저 토스트 (Polling 없음) | 실시간 이벤트의 `schemaVersion`으로 버전별 처리 가능 |
| **System Integration** | C#·Python·TypeScript 3개 언어, 공통 엔벨로프 스키마 기반 계약 + `IAlertNotifier` 신규 포트 | `CONTRACT.md`로 public/internal 분류, `FIELD-MAPPING.md`로 매핑 명시 |
| **Performance** | Docker Compose infra/app 분리, MongoDB 인덱스, Kafka 비동기, SSE로 불필요한 Polling 제거 | 토픽 분리로 소비자 불필요 메시지 처리 제거 |
| **Transition** | Hexagonal `IAlertStore` → InMemory → **Mongo 교체**, `IAlertNotifier` → SSE 어댑터 (두 포트 독립 교체) | 스키마 버전 관리로 레거시 → 신규 점진적 마이그레이션 지원 |

---

## 7. 참고 문서

- [2026-03-27-development-plan.md](2026-03-27-development-plan.md) — 다음 개발 계획 (Phase 10: 속성 타입 시스템)
- [governance-roadmap.md](governance-roadmap.md) — 거버넌스 프로젝트 연결 로드맵 (계약 검증, 스키마 버전 관리, CI)
- [2026-02-22-development-plan.md](2026-02-22-development-plan.md) — 이전 개발 계획 (Phase 6~7c, UI-1)
- [2026-02-20-development-plan.md](2026-02-20-development-plan.md) — 초기 개발 계획 (Phase 1~5)
- [temp.phases.md](../../temp.phases.md) — Phase 상세 로드맵
- [temp.uiux.integration.phases.md](../../temp.uiux.integration.phases.md) — UI/UX 통합 Phase
- [event-types.md](../shared/event-types.md) — 이벤트 타입 Command/Observation 분류
- [event-replay-contract.md](../shared/event-replay-contract.md) — Replay 계약
- [simulation-engine-architecture.md](../backend/simulation-engine-architecture.md) — Simulation 모듈 구조

---
