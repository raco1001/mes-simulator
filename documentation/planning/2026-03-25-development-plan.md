# 개발 계획 (2026-03-25)

본 문서는 Factory MES 프로젝트의 **데이터 계약 정합성 확보**, **애플리케이션 완성도 보강**, **데이터 거버넌스 프로젝트 연결 준비**를 위한 추가 개발 계획을 정리한 것입니다. 기술 면접(2026-03-30) 대비와 후속 데이터 거버넌스 프로젝트의 대상 서비스 준비를 병행합니다.

---

## 1. 배경

### 현재 상태 진단

Phase 7c.1 + UI-1까지 완료된 시점에서, 시뮬레이션 → Kafka → Pipeline → Alert → REST API 경로가 end-to-end로 동작한다. 그러나 `shared/` 디렉토리의 스키마가 **문서 저장소** 수준에 머물러 있고, 실제 코드와의 괴리(drift)가 누적되어 있다.

| 문제 영역 | 현상 |
| --- | --- |
| 스키마 누락 | `simulation.state.updated`(가장 많이 사용되는 이벤트)에 스키마 파일 없음 |
| 엔벨로프 drift | 코드가 발행하는 `runId` 필드가 스키마에 미정의 |
| 토픽 불일치 | `topics.json`에 3개 토픽 정의, 실제 사용 1개 |
| API 스키마 비표준 | JSON Schema + 커스텀 `paths` 혼합 — OpenAPI 3.x 아님 |
| 검증 부재 | 어떤 서비스도 `shared/*.json`으로 런타임/테스트 검증을 하지 않음 |
| UI 미연결 | Alert API가 존재하지만 프론트엔드에서 호출하지 않음 |
| 저장소 휘발 | `InMemoryAlertStore` — 서버 재시작 시 알람 소실 |
| Redis 미사용 | 인프라에 Redis 컨테이너가 있지만 코드에서 사용하지 않음 |

### 목표

1. **`shared/`를 Single Source of Truth(SSoT)로 전환** — 스키마가 진실이고, 코드가 이를 참조·검증하는 구조
2. **데이터 흐름 end-to-end 완성** — Asset → Kafka → Pipeline → Alert → API → UI
3. **거버넌스 프로젝트의 대상 서비스 요건 충족** — 스키마 레지스트리, 계약 검증 포인트, 버전 관리 기반 확보

### 제약

- 면접 전(D-5)까지 Phase 8, 9 완료 목표 — 스키마 정합성 + 애플리케이션 완성도
- 기존 동작하는 코드의 기능 유지 (breaking change 최소화)
- 거버넌스 프로젝트 연결 준비(Phase 10, 11)는 면접 후 진행

---

## 2. 이번 계획 범위

| Phase | 제목 | 우선순위 | 목표 시점 |
| --- | --- | --- | --- |
| 8.1 | 이벤트 엔벨로프 공통 스키마 정의 | P0 | 면접 전 |
| 8.2 | `simulation.state.updated` 스키마 추가 | P0 | 면접 전 |
| 8.3 | 공개(public) vs 내부(internal) 이벤트 분류 | P0 | 면접 전 |
| 8.4 | `topics.json` 현실 반영 | P0 | 면접 전 |
| 8.5 | API 스키마 OpenAPI 3.x 표준화 | P1 | 면접 전 |
| 8.6 | 필드명 매핑 문서 (Kafka ↔ REST) | P1 | 면접 전 |
| 9.1 | Alert SSE 스트림 — Backend (새 포트·채널·엔드포인트) | P0 | 면접 전 |
| 9.2 | Alert SSE 스트림 — Frontend (EventSource·토스트) | P0 | 면접 전 |
| 9.3 | `MongoAlertStore` 구현 (어댑터 교체) | P0 | 면접 전 |
| 9.4 | Alert 토픽 분리 (`factory.asset.alert`) + Redis 제거 | P1 | 면접 전 |
| 10.1 | Pipeline: Kafka 메시지 스키마 검증 | P0 | 면접 후 |
| 10.2 | Backend: 발행 메시지 스키마 검증 (테스트) | P0 | 면접 후 |
| 10.3 | Golden File 테스트 세트 | P1 | 면접 후 |
| 10.4 | Frontend: API 응답 검증 (개발 모드) | P2 | 면접 후 |
| 11.1 | 스키마 버전 라이프사이클 정의 | P1 | 면접 후 |
| 11.2 | 호환성 체크 스크립트 | P1 | 면접 후 |
| 11.3 | CI 파이프라인 (GitHub Actions) | P1 | 면접 후 |

---

## 3. Phase 8 — 스키마 정합성 확보 ("문서 → 계약")

`shared/`가 실제 와이어 포맷과 100% 일치하게 만든다. 거버넌스 프로젝트가 이 디렉토리를 읽으면 서비스의 모든 외부 계약을 파악할 수 있는 상태를 목표로 한다.

### Phase 8.1 — 이벤트 엔벨로프 공통 스키마 정의

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
  - `schemaVersion` pattern 완화: Phase 10.1에서 런타임 검증 도입 시 소비자가 `schemaVersion` 값을 읽어 적절한 스키마 파일로 라우팅해야 하는데, `const: "v1"` 이면 v2 메시지가 엔벨로프 검증 자체에서 차단되어 DLQ 폭발로 이어짐. `pattern`으로 완화하면 엔벨로프 검증 통과 후 소비자 코드에서 버전별 분기 처리.
- **완료 기준**: 모든 이벤트 스키마가 `event-envelope.json`을 참조하고, `runId`·`schemaVersion`·`context`가 공식 필드로 정의됨. `schemaVersion`이 `pattern` 방식으로 정의됨.
- **산출물**: `event-envelope.json`, 기존 3개 스키마 수정, `KafkaEventPublisher.cs`·`asset_pipeline.py` 수정.

---

### Phase 8.2 — `simulation.state.updated` 스키마 추가

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

### Phase 8.3 — 공개(public) vs 내부(internal) 이벤트 분류

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

### Phase 8.4 — `topics.json` 현실 반영

- **목표**: 토픽 정의 파일을 실제 운영 상태와 일치시킴.
- **배경**: `topics.json`에 3개 토픽(`factory.asset.events`, `factory.asset.health`, `factory.asset.alert`)이 정의되어 있지만, 코드에서는 `factory.asset.events` 하나만 사용한다. 이벤트 목록에도 `simulation.state.updated`가 누락.
- **계획**:
  - `factory.asset.events`의 `events` 배열에 `simulation.state.updated` 추가
  - 미사용 토픽 2개에 `"status": "planned"` 필드 추가하여 현재 미활성 상태 명시
  - `topics.json` 스키마에 `status` 필드(`"active"` | `"planned"` | `"deprecated"`) 정의 추가
- **완료 기준**: `topics.json`의 active 토픽과 이벤트 목록이 실제 코드 사용과 일치.
- **산출물**: `topics.json` 수정.

---

### Phase 8.5 — API 스키마 OpenAPI 3.x 표준화

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

### Phase 8.6 — 필드명 매핑 문서 (Kafka ↔ REST)

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

## 4. Phase 9 — 애플리케이션 완성도 보강

데이터 흐름의 끝단(UI)과 저장소 내구성을 보강하여, 거버넌스가 관리할 전체 파이프라인이 실제로 동작하게 만든다.

### Phase 9.1 — Alert SSE 스트림 Backend

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
- **계획**:
  - **Driven Port**: `Application/Alert/Ports/Driven/IAlertNotifier.cs` 신규
    - `Task NotifyAsync(AlertDto alert, CancellationToken ct)`
    - `IAsyncEnumerable<AlertDto> SubscribeAsync(CancellationToken ct)` — 연결별 Channel Reader 제공
  - **Infrastructure**: `Infrastructure/Alert/SseAlertChannel.cs` (Singleton)
    - `System.Threading.Channels.Channel<AlertDto>`를 연결별로 생성·관리
    - `NotifyAsync` — 모든 활성 채널에 `WriteAsync`
    - `SubscribeAsync` — 새 `Channel` 생성 후 `ChannelReader`를 `IAsyncEnumerable`로 래핑. 연결 종료(ct 취소) 시 채널 자동 해제
  - **AlertController**: `GET /api/alerts/stream` 엔드포인트 추가
    - 응답 헤더: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`
    - `IAlertNotifier.SubscribeAsync`로 구독 → `await foreach` 루프로 `data: {json}\n\n` 전송
    - 클라이언트 연결 종료(`HttpContext.RequestAborted`) 시 루프 탈출
  - **KafkaAlertConsumerService**: `IAlertNotifier` 주입 추가, `_alertStore.Add(alert)` 직후 `await _alertNotifier.NotifyAsync(alert, ct)` 호출
  - **DI**: `Program.cs`에 `IAlertNotifier` → `SseAlertChannel` Singleton 등록
  - **CORS**: 개발 환경 기존 `AllowAnyOrigin` 유지 (SSE는 자격증명 없이 동작)
  - **테스트**: `SseAlertChannelTests` — `NotifyAsync` 후 `SubscribeAsync` reader에서 수신 검증. `KafkaAlertConsumerService` 테스트 — `IAlertNotifier.NotifyAsync` mock 호출 검증.
- **완료 기준**: `GET /api/alerts/stream` 엔드포인트가 SSE 스트림을 유지하고, Kafka Alert 수신 시 즉시 전송됨. 기존 `GET /api/alerts` (이력 조회) 동작 유지.
- **산출물**: `IAlertNotifier.cs`, `SseAlertChannel.cs`, `AlertController.cs` 수정, `KafkaAlertConsumerService.cs` 수정, `Program.cs` 수정, 테스트.

---

### Phase 9.2 — Alert SSE 스트림 Frontend

- **목표**: 백엔드 SSE 스트림을 구독하여 Alert 발생 시 Slack 알림처럼 즉시 토스트 표시. Polling 없이 실시간 반응.
- **계획**:
  - **Entity**: `entities/alert/` 디렉토리 신규
    - `model/types.ts` — `AlertDto` 인터페이스 (assetId, timestamp, severity, message, runId?, metric?, current?, threshold?, code?)
    - `api/alertStream.ts` — `EventSource` 래퍼
      - `subscribeAlerts(onAlert: (alert: AlertDto) => void): () => void` — 구독 시작 후 cleanup 함수 반환
      - 연결 끊김 시 자동 재연결 (브라우저 `EventSource` 기본 동작)
    - `api/alertApi.ts` — `getAlerts(limit?)` (이력 조회용, REST)
  - **Toast 컴포넌트**: `shared/ui/AlertToast/` 또는 `entities/alert/ui/AlertToast.tsx`
    - severity(`warning` / `error`)에 따라 색상 구분
    - 자동 닫힘 타이머 (warning: 5초, error: 10초 또는 수동 닫기)
  - **앱 연결**: `app/` 레벨에서 `subscribeAlerts`를 마운트 시 구독 — 어느 페이지에 있어도 Alert 수신
    - Alert 수신 시 Toast 큐에 추가
    - 최근 N개 Alert 상태 유지 (`pages/home/` 또는 별도 알람 패널에 목록 표시)
  - **테스트**: `alertStream.test.ts` — mock `EventSource`, `onAlert` 콜백 호출 검증. `AlertToast.test.tsx` — severity별 렌더링, 자동 닫힘 검증.
- **완료 기준**: 시뮬레이션 실행 → Pipeline 이상 탐지 → Kafka `alert.generated` → Backend SSE Push → **브라우저 토스트 즉시 표시** 전체 흐름 동작.
- **산출물**: `entities/alert/` (types, alertStream, alertApi), `AlertToast` 컴포넌트, 앱 레벨 구독 연결, 테스트.

---

### Phase 9.3 — `MongoAlertStore` 구현 (어댑터 교체)

- **목표**: `InMemoryAlertStore`를 `MongoAlertStore`로 교체하여 Alert 내구성 확보. Hexagonal 아키텍처의 "어댑터 교체" 가치를 코드로 증명.
- **배경**: 현재 `IAlertStore` → `InMemoryAlertStore`(Singleton)로, 서버 재시작 시 모든 알람이 소실된다. `IAlertStore` 인터페이스가 이미 존재하므로 구현체만 추가하면 된다. SSE Push(Phase 9.1)는 실시간 전달이고, `IAlertStore`는 이력 조회 목적이므로 두 포트는 독립적으로 교체 가능.
- **계획**:
  - **Infrastructure**: `Infrastructure/Alert/MongoAlertStore.cs` 신규 — `alerts` 컬렉션에 AlertDto를 BSON 문서로 저장·조회
  - **Mongo 모델**: `MongoAlertDocument` 정의 (AssetId, Timestamp, Severity, Message, RunId, Metric, Current, Threshold, Code, Metadata)
  - **DI 전환**: `Program.cs`에서 `IAlertStore` 등록을 `MongoAlertStore`로 변경 (Scoped)
  - **인프라**: `infrastructure/mongo/MODEL.md`에 `alerts` 컬렉션 스키마 추가
  - **테스트**: `MongoAlertStoreTests` (mock 기반 단위 테스트)
- **완료 기준**: Alert가 MongoDB에 저장되고, 서버 재시작 후에도 `GET /api/alerts`로 이력 조회 가능. `Program.cs`에서 DI 한 줄 변경으로 `InMemoryAlertStore` ↔ `MongoAlertStore` 전환 가능.
- **산출물**: `MongoAlertStore.cs`, `MongoAlertDocument.cs`, `MODEL.md` 수정, `Program.cs` 수정, 테스트.
- **포트폴리오 확장 제안 (선택)**:
  - `IAlertStore` 구현체를 3단계로 제시:
    - `InMemoryAlertStore` — 개발/실험용 (휘발성, 가장 단순)
    - `FileAlertStore` — 로컬 영속/복원용 (포트폴리오 데모 친화적)
    - `MongoAlertStore` — 운영 유사 환경용
  - 설정 기반 DI 전환(`AlertStore:Provider = InMemory | File | Mongo`)으로 코드 변경 없이 어댑터 교체 가능하도록 설계
  - `FileAlertStore` 트레이드오프를 문서화:
    - 장점: 로컬 영속성, 재시작 복원, 인프라 의존도 낮음
    - 한계: 파일 잠금/동시성, 검색 성능, 다중 인스턴스 확장성 제한
  - 메시지 포인트: “Hexagonal 아키텍처에서 포트는 고정, 요구사항에 따라 어댑터만 교체”

---

### Phase 9.4 — Alert 토픽 분리 + Redis 제거

- **목표**: Alert 이벤트를 전용 Kafka 토픽으로 분리하여 토픽 설계를 정리하고, 사용하지 않는 Redis를 인프라에서 제거.
- **배경**:
  - **토픽 분리**: 현재 Pipeline이 `alert.generated`를 `factory.asset.events`에 섞어 발행하고, `KafkaAlertConsumerService`가 해당 토픽의 모든 메시지를 읽어 `eventType == "alert.generated"`만 필터링한다. 시뮬레이션 틱마다 발행되는 다수의 이벤트를 소비자가 불필요하게 훑는 구조다. `topics.json`에 `factory.asset.alert` 토픽이 이미 정의되어 있었으나 미사용 상태였다.
  - **Redis 제거**: `docker-compose.infra.yml`에 Redis + Redis Commander가 정의되어 있지만 코드에서 전혀 사용하지 않는다. 이 프로젝트는 단일 인스턴스 포트폴리오 규모이므로 캐싱(단일 클라이언트)과 멀티 인스턴스 SSE 동기화(단일 인스턴스) 모두 필요하지 않다. 규모 확장 시 Redis Pub/Sub으로 멀티 인스턴스 SSE 동기화가 가능하다는 설계 근거를 문서로 남기는 것으로 대체.
- **계획**:
  - **[토픽 분리] Pipeline**: `settings.py`에 `kafka_topic_alert_events: str = "factory.asset.alert"` 추가. `asset_worker.py`의 alert 발행 토픽을 `kafka_topic_asset_events` → `kafka_topic_alert_events`로 변경.
  - **[토픽 분리] Backend `KafkaOptions.cs`**: `TopicAlertEvents: string = "factory.asset.alert"` 추가.
  - **[토픽 분리] `KafkaAlertConsumerService.cs`**: 구독 토픽을 `TopicAlertEvents`로 변경. `factory.asset.alert`의 모든 메시지가 alert이므로 `eventType` 필터링 로직 제거.
  - **[토픽 분리] `topics.json`**: `factory.asset.alert` 토픽 `"status": "active"`로 전환. `factory.asset.events`의 `events` 목록에서 `alert.generated` 제거.
  - **[Redis 제거] `docker-compose.infra.yml`**: `redis`, `redis-commander` 서비스 제거.
  - **[Redis 제거] `docker-compose.full.yml`**: 동일하게 Redis 서비스 제거.
  - **[확장 근거 문서화]** `documentation/backend/simulation-engine-architecture.md` 또는 ADR에 "Redis Pub/Sub을 활용한 멀티 인스턴스 SSE 동기화" 설계 근거 한 섹션 추가.
  - **테스트**: Pipeline `test_asset_worker.py` — alert 발행 토픽이 `kafka_topic_alert_events`임을 검증. Backend `KafkaAlertConsumerServiceTests` — `TopicAlertEvents` 구독 및 필터링 제거 반영.
- **완료 기준**: Alert 이벤트가 `factory.asset.alert` 토픽으로만 발행·소비되고, `factory.asset.events`에는 시뮬레이션 이벤트만 흐른다. Redis 서비스가 Compose에서 제거되어 인프라와 코드가 일치한다.
- **설계 근거**:
  - 토픽 분리: 소비자가 관심 없는 메시지를 읽고 버리는 불필요한 처리 제거. 토픽 = 이벤트 계약이라는 Kafka 표준 패턴 준수.
  - Redis 제거: 현재 규모에서 명확한 용도 없이 인프라 복잡도만 높이는 컴포넌트 제거. "코드에 없는 인프라"는 거버넌스 관점에서 "관리 대상인데 계약이 없는 것"과 동일.
  - 확장 경로: 다중 인스턴스 배포 시 `IAlertNotifier` 구현체를 `SseAlertChannel`(메모리) → `RedisBackedSseChannel`(Redis Pub/Sub)으로 교체하면 SSE 실시간성 유지 가능. 인터페이스 변경 없음.
- **산출물**: `settings.py` 수정, `asset_worker.py` 수정, `KafkaOptions.cs` 수정, `KafkaAlertConsumerService.cs` 수정, `topics.json` 수정, `docker-compose.infra.yml` 수정, `docker-compose.full.yml` 수정, 확장 근거 문서, 테스트 수정.

---

## 5. Phase 10 — 계약 준수 검증 ("코드가 스키마를 참조하게")

거버넌스 프로젝트가 "계약 위반을 탐지"하려면, 각 서비스에 검증 포인트가 심어져 있어야 한다.

### Phase 10.1 — Pipeline: Kafka 메시지 스키마 검증

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

### Phase 10.2 — Backend: 발행 메시지 스키마 검증 (테스트)

- **목표**: 백엔드가 Kafka로 발행하는 메시지가 `shared/event-schemas` 스키마에 부합하는지 테스트 시점에 자동 검증.
- **계획**:
  - 테스트 프로젝트에 `NJsonSchema` 또는 `JsonSchema.Net` 패키지 추가
  - `SchemaValidationHelper` 유틸리티 — `shared/event-schemas/schemas/*.json` 로딩, `Validate(jsonString, schemaPath)` 메서드
  - 시뮬레이션 핸들러 테스트에서: mock `IEventPublisher`가 캡처한 `EventDto`를 JSON 직렬화 → `SchemaValidationHelper`로 검증 어설션 추가
  - 기존 테스트 통과 유지하며 스키마 검증 어설션만 추가
- **완료 기준**: 시뮬레이션·알람 관련 테스트가 스키마 검증을 포함하고, 스키마 불일치 시 테스트 실패.
- **산출물**: `SchemaValidationHelper`, 기존 테스트 파일 수정, NuGet 패키지 추가.

---

### Phase 10.3 — Golden File 테스트 세트

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

### Phase 10.4 — Frontend: API 응답 검증 (개발 모드)

- **목표**: 개발 환경에서 API 응답이 OpenAPI 스키마에 부합하는지 자동 경고.
- **계획**:
  - `shared/api/` 또는 `shared/lib/`에 `responseValidator.ts` — `openapi.json`의 `components/schemas` 기반 zod 스키마 또는 ajv 검증
  - `httpClient.ts` 인터셉터에 개발 모드 분기: `import.meta.env.DEV`일 때만 응답 검증, 실패 시 `console.warn`
  - 프로덕션 빌드에서는 제거 (tree-shaking 또는 조건부 import)
- **완료 기준**: 개발 모드에서 스키마 불일치 API 응답 시 콘솔 경고 출력. 프로덕션 번들에 검증 코드 미포함.
- **산출물**: `responseValidator.ts`, `httpClient.ts` 수정.

---

## 6. Phase 11 — 거버넌스 프로젝트 연결 준비

이 단계를 완료하면, 후속 데이터 거버넌스 프로젝트가 Factory MES를 대상 서비스로 즉시 활용할 수 있다.

### Phase 11.1 — 스키마 버전 라이프사이클 정의

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

### Phase 11.2 — 호환성 체크 스크립트

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

### Phase 11.3 — CI 파이프라인 (GitHub Actions)

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

## 7. 거버넌스 프로젝트와의 인터페이스

위 Phase 완료 후, 거버넌스 프로젝트가 Factory MES에서 읽을 수 있는 것:

| 거버넌스 기능 | Factory MES 제공물 | Phase |
| --- | --- | --- |
| 스키마 레지스트리 | `shared/event-schemas/versions/*.json` + `shared/api-schemas/openapi.json` | 8.1~8.5 |
| 계약 범위 정의 | `CONTRACT.md` (public/internal 분류) + `FIELD-MAPPING.md` | 8.3, 8.6 |
| 운영 메타데이터 확장 | 엔벨로프 `context` 필드 (traceId, correlationId 등 자유 확장) | 8.1 |
| 다중 버전 공존 | `schemaVersion` pattern 기반 소비자 라우팅, `VERSIONING.md` 전략 | 8.1, 11.1 |
| 계약 위반 탐지 | Pipeline DLQ (`dead_letter_events`), Backend 테스트 스키마 검증 | 10.1~10.2 |
| 호환성 분석 | `check-compatibility.py` 출력, `VERSIONING.md` 규칙 | 11.1~11.2 |
| 서비스 간 데이터 흐름 | `topics.json` (토픽·이벤트 매핑) + `FIELD-MAPPING.md` | 8.4, 8.6 |
| 변경 이력 | `CHANGELOG.md` + git diff on `shared/` | 11.1 |
| 자동 검증 | GitHub Actions CI (`ci.yml`) | 11.3 |

---

## 8. 면접 대비 요약

Phase 8 + 9 완료 시 면접에서 꺼낼 수 있는 이야기:

| JD 키워드 | Factory MES 증명 | 스키마 거버넌스 보강 |
| --- | --- | --- |
| **Real-time** | SimulationEngine → Kafka → Pipeline → **SSE Push** → 브라우저 토스트 (Polling 없음) | 실시간 이벤트의 `schemaVersion`으로 버전별 처리 가능 |
| **System Integration** | C#·Python·TypeScript 3개 언어, 공통 엔벨로프 스키마 기반 계약 + `IAlertNotifier` 신규 포트 | `CONTRACT.md`로 public/internal 분류, `FIELD-MAPPING.md`로 매핑 명시 |
| **Performance** | Docker Compose infra/app 분리, MongoDB 인덱스, Kafka 비동기, SSE로 불필요한 Polling 제거 | Pipeline DLQ로 계약 위반 메시지 격리, 정상 처리 경로 보호 |
| **Transition** | Hexagonal `IAlertStore` → InMemory → **Mongo 교체**, `IAlertNotifier` → SSE 어댑터 (두 포트 독립 교체) | 스키마 버전 관리로 레거시 → 신규 점진적 마이그레이션 지원 |

---

## 9. 참고 문서

- [2026-02-22-development-plan.md](2026-02-22-development-plan.md) — 이전 개발 계획 (Phase 6~7c, UI-1)
- [2026-02-20-development-plan.md](2026-02-20-development-plan.md) — 초기 개발 계획 (Phase 1~5)
- [temp.phases.md](../../temp.phases.md) — Phase 상세 로드맵
- [temp.uiux.integration.phases.md](../../temp.uiux.integration.phases.md) — UI/UX 통합 Phase
- [event-types.md](../shared/event-types.md) — 이벤트 타입 Command/Observation 분류
- [event-replay-contract.md](../shared/event-replay-contract.md) — Replay 계약
- [simulation-engine-architecture.md](../backend/simulation-engine-architecture.md) — Simulation 모듈 구조

---
