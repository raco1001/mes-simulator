# 개발 계획 (2026-02-22)

본 문서는 이번 세션에서 계획하고 완수한 **Phase 6.1 ~ 6.5**, **Phase 7a.1 ~ 7a.2**, **Phase 7b.1 ~ 7b.2**, **Phase 7c.1** 내용을 정리한 것입니다. 상세 단계 정의는 [temp.phases.md](../../temp.phases.md)를 참고합니다.

---

## 1. 이번 세션 범위

| Phase   | 제목                               | 상태   |
| ------- | ---------------------------------- | ------ |
| 6.1     | SimulationRun Status + Repository | 완료   |
| 6.2     | 전파 1회 로직 추출                  | 완료   |
| 6.2.5   | 에셋별 tick 스키마·엔진 규칙 확정   | 완료   |
| 6.4     | BackgroundService (due 기반 tick)  | 완료   |
| 6.5     | 지속 Run API (start / stop)        | 완료   |
| 7a.1    | 이벤트 타입 분리 (Command/Observation) | 완료   |
| 7a.2    | 상태 변경을 apply(event) 경로로 수렴   | 완료   |
| 7b.1    | 파이프라인: 이상 시 AlertEvent 발행   | 완료   |
| 7b.2    | 백엔드: AlertEvent 소비 → 클라이언트 전달 | 완료   |
| 7c.1    | Replay 모드: runId(·tick 상한) 기준 재실행/재생 | 완료   |
| UI-1    | 캔버스 기반 에셋·관계 편집 (기초)                | 완료   |

---

## 2. 완료한 단계 요약

### Phase 6.1 — SimulationRun에 Status 추가 + Repository 확장

- **목표**: Run을 "진행 중/종료됨"으로 구분하고, 엔진이 Running인 Run만 조회할 수 있게 함.
- **계획·완료 내용**:
  - **규약**: `SimulationRunStatus` enum (Pending, Running, Stopped, Completed), `SimulationRunDto.Status` 추가, `ISimulationRunRepository`에 `GetRunningAsync`, `UpdateStatusAsync` 추가. `EndAsync` 호출 시 Status=Completed + EndedAt 설정으로 규약 명시.
  - **테스트**: `RunSimulationCommandHandler` 단위 테스트 — Create 시 Status=Pending, 전파 시작 직후 UpdateStatusAsync(Running), 종료 시 EndAsync 1회 검증.
  - **구현**: `MongoSimulationRunDocument.Status`, `MongoSimulationRunRepository` (ToDto/ToDocument Status 매핑, GetRunningAsync, UpdateStatusAsync, EndAsync에 Status=Completed), Handler에서 Run 생성 시 Pending → Create 직후 UpdateStatusAsync(Running) → 전파 후 EndAsync.
- **산출물**: SimulationRunStatus.cs, SimulationRunDto.cs, ISimulationRunRepository.cs, MongoSimulationRunDocument/Repository, RunSimulationCommandHandler 수정.

---

### Phase 6.2 — 전파 1회 로직 추출 (Run 생성/종료와 분리)

- **목표**: "주어진 Run에 대해 BFS 전파 1회만 수행"하는 진입점을 만들어, 단건 API와 이후 tick 엔진이 동일 로직을 재사용하게 함.
- **계획·완료 내용**:
  - **규약**: `IRunSimulationCommand`에 `Task RunOnePropagationAsync(string runId, RunSimulationRequest request, CancellationToken)` 추가. Run은 호출 전에 이미 존재한다고 가정, Run 생성/End 호출 없음.
  - **테스트**: RunOnePropagationAsync 호출 시 CreateAsync/UpdateStatusAsync/EndAsync 미호출 검증. 전파 수행 시 GetStateByAssetIdAsync, UpsertStateAsync, AppendAsync, PublishAsync 호출 검증. 기존 RunAsync 테스트 유지( Create Pending, UpdateStatusAsync Running, EndAsync 1회 ).
  - **구현**: Handler에 `RunOnePropagationAsync` 구현(BFS·상태 병합·이벤트 append·Publish만 수행). `RunAsync`는 Run 생성 → UpdateStatusAsync(Running) → **RunOnePropagationAsync 1회** → EndAsync → RunResult 반환.
- **산출물**: IRunSimulationCommand.cs, RunSimulationCommandHandler.cs, RunSimulationCommandHandlerTests.cs. SimulationController 변경 없음.

---

### Phase 6.2.5 — 에셋별 tick 스키마·엔진 규칙 확정

- **목표**: 개별 상태 머신 기준 이벤트 발생을 위해, 에셋별 tick 주기를 스키마와 엔진 규칙에 미리 반영. 6.4에서 전역 tick vs due 기반 구현 선택 가능하게 함.
- **계획·완료 내용**:
  - **에셋 tick 스키마**: AssetDto.Metadata에 `tickIntervalMs`(선택, 0/미설정 시 Run 전역 tick), `tickPhaseMs`(선택, 오프셋) 정의. 문서에 타입·의미·기본 동작 명시.
  - **엔진 규칙 문서화**: 매 스텝 "due 에셋만 수집 → due 에셋만 update/전파". due 판단(현재 시각 또는 Run tick, tickIntervalMs, lastTick). 전역 tick 모드 = due = 참여 에셋 전체. 6.4 설계 방향 = 루프 "due 수집 → due만 전파", 첫 구현은 due = 전체 가능.
  - **코드 변경**: 없음(문서만 반영).
- **산출물**: [simulation-engine-tick-rules.md](../backend/simulation-engine-tick-rules.md) 신규, [api-schemas.md](../shared/api-schemas.md)에 AssetDto metadata tick 규약 추가, [simulation-api.md](../backend/simulation-api.md) 참고 링크.

---

### Phase 6.4 — BackgroundService (due 기반 tick 루프)

- **목표**: Status=Running인 Run에 대해 주기적으로 전파·이벤트 발생. **due 기반**으로 "due 에셋 수집 → due 에셋만 전파" 구조로 구현.
- **계획·완료 내용**:
  - **TickIndex**: SimulationRunDto/MongoSimulationRunDocument에 `TickIndex`(int, 기본 0) 추가. `ISimulationRunRepository.UpdateTickIndexAsync(string id, int tickIndex)` 추가 및 Mongo 구현. Run 생성 시 TickIndex=0 설정.
  - **Due 규칙**: 참여 에셋 = Run TriggerAssetId 기준 BFS(GetOutgoingAsync). due 판단 = metadata tickIntervalMs 없거나 0 → 항상 due; 있으면 lastTick(State.UpdatedAt ?? Run.StartedAt), (now - lastTick).TotalMilliseconds >= tickIntervalMs이면 due. 전역 tick = due 수 == 참여 수 → RunOnePropagationAsync(runId, run 기반 request) 1회. 에셋별 tick = due가 참여의 일부 → due마다 RunOnePropagationAsync(runId, TriggerAssetId=dueId, MaxDepth=0).
  - **SimulationEngineService**: BackgroundService, IServiceScopeFactory로 스코프 생성 후 Repository/Command/AssetRepository/RelationshipRepository resolve. ExecuteAsync: 주기(1초) 루프, GetRunningAsync → 각 Run에 대해 참여 에셋 BFS, due 수집, UpdateTickIndexAsync(run.TickIndex+1), 전역이면 RunOnePropagationAsync 1회, 아니면 due별 RunOnePropagationAsync(MaxDepth=0). 한 Run 예외 시 다른 Run 계속 처리.
  - **등록**: Program.cs에 `AddHostedService<SimulationEngineService>` 등록.
- **산출물**: SimulationRunDto(TickIndex), MongoSimulationRunDocument, ISimulationRunRepository(UpdateTickIndexAsync), SimulationEngineService(Workers/ 또는 Application/Simulation/), Program.cs(AddHostedService). 테스트 Mock에 UpdateTickIndexAsync 설정 및 Create 시 TickIndex=0 검증 추가.

---

### Phase 6.5 — 지속 Run API (start / stop)

- **목표**: 사용자가 "지속 시뮬레이션 시작"과 "중단"을 API로 수행할 수 있게 함. Run을 Running으로 생성한 뒤 엔진이 tick마다 전파·이벤트를 발생시키고, stop 시 해당 Run만 제외.
- **계획·완료 내용**:
  - **API**: `POST /api/simulation/runs/start` — Body `{ triggerAssetId, patch?, maxDepth? }`. Run 생성(Status=**Running**, EndedAt=null), **전파는 호출하지 않음**. runId 반환. 동시에 Running인 Run이 1개를 넘으면 409 Conflict + Result(success: false, message). `POST /api/simulation/runs/{runId}/stop` — 해당 Run의 Status=**Stopped**, EndedAt=UtcNow 설정. 엔진(GetRunningAsync)에서 제외되어 tick 중단. 기존 `POST /api/simulation/runs` 단건 1회 전파 후 완료 유지.
  - **Contract**: `IStartContinuousRunCommand`(StartAsync(RunSimulationRequest) → StartContinuousRunResult), `IStopSimulationRunCommand`(StopAsync(runId) → StopSimulationRunResult). DTO: StartContinuousRunResult(Success, RunId, Message?), StopSimulationRunResult(Success, Message?). Request는 기존 RunSimulationRequest 재사용.
  - **테스트**: StartContinuousRunCommandHandlerTests — CreateAsync에 Status=Running·EndedAt=null 전달, 전파 미호출 검증; GetRunningAsync().Count >= 1일 때 Start 실패(Success=false) 검증. StopSimulationRunCommandHandlerTests — Run 존재·Running일 때 UpdateStatusAsync(Stopped, EndedAt) 호출; Run 없음 시 실패; 이미 Stopped인 Run에 stop 시 idempotent 성공.
  - **구현**: StartContinuousRunCommandHandler — GetRunningAsync 후 1개 이상이면 Result 실패 반환, 아니면 SimulationRunDto(Status=Running, EndedAt=null, TickIndex=0) CreateAsync만 호출. StopSimulationRunCommandHandler — GetByIdAsync 후 null이면 실패, 아니면 UpdateStatusAsync(runId, Stopped, UtcNow). SimulationController에 POST runs/start, POST runs/{runId}/stop 액션 추가. Program.cs에 두 Command Scoped 등록.
- **산출물**: IStartContinuousRunCommand, IStopSimulationRunCommand, StartContinuousRunResult, StopSimulationRunResult, StartContinuousRunCommandHandler, StopSimulationRunCommandHandler, StartContinuousRunCommandHandlerTests, StopSimulationRunCommandHandlerTests, SimulationController·Program.cs 수정.

---

### Application/Simulation 구조 정리 (DDD/Hexagonal)

- **목표**: Application/Simulation 최상위에 섞여 있던 도메인 타입(enum·상수)과 서비스 로직을 레이어별로 분리.
- **계획·완료 내용**:
  - **Domain/Simulation**: `EventKind`, `SimulationRunStatus` → `Domain/Simulation/ValueObjects/`, `EventTypes` → `Domain/Simulation/Constants/`. 네임스페이스 `DotnetEngine.Domain.Simulation.ValueObjects`, `DotnetEngine.Domain.Simulation.Constants`. Application·Infrastructure·Tests에서 해당 타입 참조 시 새 네임스페이스 사용하도록 수정.
  - **Workers 폴더**: `SimulationEngineService`를 `Application/Simulation/Workers/SimulationEngineService.cs`로 이동, 네임스페이스 `DotnetEngine.Application.Simulation.Workers`. Program.cs에서 `using DotnetEngine.Application.Simulation.Workers` 추가.
- **산출물**: Domain/Simulation/ValueObjects(EventKind, SimulationRunStatus), Domain/Simulation/Constants(EventTypes), Application/Simulation/Workers(SimulationEngineService), 기존 Application/Simulation 루트의 해당 파일 삭제, 참조·README 수정.

---

### Phase 7a.1 — 이벤트 타입 분리 (Command / Observation)

- **목표**: Live 전환 시 "명령부만 교체"가 가능하도록 이벤트를 입력(Command)과 출력(Observation)으로 구분. 타입·문서만 정리, 동작 변경 없음.
- **계획·완료 내용**:
  - **문서**: `documentation/shared/event-types.md` 신규 — EventKind(Command/Observation) 정의, Observation 타입(simulation.state.updated, power_changed, state_transitioned), Command 타입(start_machine, stop_machine, change_speed), "시뮬레이터는 Observation만 생성, Live 시 Adapter가 Observation 주입" 계약. `event-replay-contract.md`에 eventType 분류 및 event-types.md 참조 한 줄 추가.
  - **코드**: EventKind enum, EventTypes 상수(Observation/Command) 및 `GetKind(string? eventType)` 헬퍼 — Domain/Simulation으로 이동 후 위 구조 정리와 통합. RunSimulationCommandHandler, ConnectedToRule, ContainsRule, SuppliesRule에서 "simulation.state.updated" → `EventTypes.SimulationStateUpdated` 공통 상수 사용.
  - **테스트**: EventTypesTests(GetKind 검증), 기존 시뮬레이션 테스트 통과 유지.
- **산출물**: event-types.md, event-replay-contract.md 수정, Domain/Simulation(EventKind, EventTypes), Rules·Handler 상수 마이그레이션, EventTypesTests.

---

### Phase 7a.2 — 상태 변경을 apply(event) 경로로 수렴

- **목표**: 엔진의 상태 변경 진입점을 apply(event) 하나로 모아, Live 교체 시 동일 엔진을 쓸 수 있게 함.
- **계획·완료 내용**:
  - **Port**: `IEngineStateApplier` (Driven) — `Task ApplyAsync(EventDto evt, StateDto mergedState, CancellationToken)`. "상태 저장 + 이벤트 저장·발행" 단일 진입점.
  - **구현체**: `EngineStateApplier` — UpsertStateAsync(mergedState) → AppendAsync(evt) → PublishAsync(evt) 순서. Application/Simulation/EngineStateApplier.cs.
  - **Handler 리팩터**: RunSimulationCommandHandler 노드 처리 블록에서 기존 UpsertStateAsync + AppendAsync + PublishAsync 제거 후 `_applier.ApplyAsync(nodeEvent, mergedState, cancellationToken)` 한 번 호출로 대체. 규칙 이벤트는 기존처럼 Append/Publish만 유지(대상 상태는 해당 노드 dequeue 시 apply로 갱신).
  - **DI**: Program.cs에 IEngineStateApplier → EngineStateApplier Scoped 등록. 테스트 CreateHandler에서 EngineStateApplier(동일 mock 주입) 사용해 Append/Publish 검증 유지.
- **산출물**: IEngineStateApplier.cs, EngineStateApplier.cs, RunSimulationCommandHandler 수정, Program.cs·RunSimulationCommandHandlerTests 수정.

---

### Phase 7b.1 — 파이프라인: 이상 시 AlertEvent 발행

- **목표**: 정상 텔레메트리는 DB 저장만, **이상(WARNING/ERROR) 시에만** `alert.generated` 이벤트를 Kafka에 produce.
- **계획·완료 내용**:
  - **Kafka Producer**: `servers/pipeline`에 `messaging/provider/kafka_producer.py`(AssetEventProducer) 추가. Settings 기반 bootstrap, JSON 직렬화, `send(topic, value=dict)`, `close()`. 토픽은 기존 `factory.asset.events` 재사용.
  - **Alert payload**: `pipelines/asset_pipeline.py`에 `build_alert_event(asset_id, timestamp, status, current_temp, current_power, run_id=None)` 추가. eventType=alert.generated, payload.severity(warning/error), message, metric/current/threshold/code, metadata.runId(있으면). 스키마(shared/event-schemas/schemas/alert.generated.json) 필수 필드 충족.
  - **Worker**: `asset_worker.py`에서 `process_health_updated` / `process_simulation_state_updated` 후 `state.status in (WARNING, ERROR)`일 때만 `build_alert_event` 호출 후 `producer.send(kafka_topic_asset_events, value=alert_payload)`. shutdown 시 producer.close().
  - **테스트**: test_asset_pipeline.py에 TestBuildAlertEvent(WARNING/ERROR severity·runId), test_asset_worker.py에 mock producer로 WARNING 시 send 1회·NORMAL 시 미호출·simulation.state.updated ERROR 시 send 검증.
- **산출물**: messaging/provider/kafka_producer.py, __init__.py, asset_pipeline.build_alert_event, asset_worker 수정, tests/pipelines/test_asset_pipeline.py, tests/workers/test_asset_worker.py.

---

### Phase 7b.2 — 백엔드: AlertEvent 소비 → 클라이언트 전달

- **목표**: 파이프라인이 발행한 알람을 백엔드가 Kafka에서 소비해 **REST API**로 최신 알람 목록 제공. (실시간 푸시는 REST만으로 완료 기준 충족.)
- **계획·완료 내용**:
  - **Application**: AlertDto(AssetId, Timestamp, Severity, Message, RunId, Metric, Current, Threshold, Code, Metadata), IGetAlertsQuery(GetLatestAsync(limit?, ct)), IAlertStore(Add, GetLatest(maxCount)). GetAlertsQueryHandler — IAlertStore.GetLatest(limit ?? 50) 반환.
  - **Infrastructure**: InMemoryAlertStore(thread-safe, 최근 N건·기본 100). KafkaOptions에 ConsumerGroupId(backend-alert-consumer) 추가. KafkaAlertConsumerService(HostedService) — factory.asset.events 구독, eventType==alert.generated만 파싱해 AlertDto로 IAlertStore.Add.
  - **Presentation**: AlertController, GET /api/alerts?limit= (optional, default 50). Program.cs에 IAlertStore(Singleton), IGetAlertsQuery(Scoped), AddHostedService<KafkaAlertConsumerService>.
  - **테스트**: GetAlertsQueryHandlerTests(mock store, 반환·limit 검증), AlertControllerTests(StubAlertStore로 200·빈 목록·1건 목록·limit 쿼리 검증).
- **산출물**: Application/Alert(Dto, Ports, GetAlertsQueryHandler), Infrastructure/Alert/InMemoryAlertStore, Infrastructure/Kafka/KafkaAlertConsumerService, KafkaOptions 수정, Presentation/Controllers/AlertController, Program.cs, DotnetEngine.Tests(GetAlertsQueryHandlerTests, AlertControllerTests).

---

### Phase 7c.1 — Replay 모드: runId(·tick 상한) 기준 재실행/재생

- **목표**: 저장된 이벤트로 "Run X 재생" 또는 "Run X, Tick N까지" 재실행·시각화. 디지털 트윈·원인 분석·what-if에 사용.
- **계획·완료 내용**:
  - **이벤트 조회**: EventDto·MongoEventDocument에 RunTick(int?) 추가. Append 시 dto.RunTick ?? payload["tick"] 저장. IEventRepository.GetBySimulationRunIdAsync(simulationRunId, tickMax?, ct) — tickMax 지정 시 RunTick <= tickMax 필터, 정렬 RunTick → OccurredAt → Timestamp. RunSimulationCommandHandler·SuppliesRule/ContainsRule/ConnectedToRule에서 EventDto 생성 시 RunTick 설정.
  - **GET /api/simulation/runs/{runId}/events**: 쿼리 파라미터 tickMax(optional) 추가. repository에 tickMax 전달해 "Tick N까지" 이벤트만 조회 가능.
  - **재실행(Replay)**: IReplayRunCommand(ReplayAsync(runId, tickMax?, ct) → ReplayRunResult). ReplayRunCommandHandler — run 존재 확인 → GetBySimulationRunIdAsync(runId, tickMax) → eventType==simulation.state.updated만 필터 → payload에서 StateDto 복원 → IAssetRepository.UpsertStateAsync만 호출(이벤트 재기록·Kafka 발행 없음). POST /api/simulation/runs/{runId}/replay?tickMax= (optional). 200 시 ReplayRunResult(Success, RunId, TickMax, EventsApplied, Message), run 없으면 404.
  - **테스트**: ReplayRunCommandHandlerTests — run 없음 시 실패, simulation.state.updated 1건 시 UpsertStateAsync 1회·payload 매핑 검증, tickMax 전달 시 repository 인자 검증.
- **산출물**: EventDto.RunTick, MongoEventDocument.RunTick, IEventRepository(tickMax), MongoEventRepository(GetBySimulationRunIdAsync tickMax·정렬), RunSimulationCommandHandler·Rules RunTick 설정, SimulationController(GetRunEvents tickMax, ReplayRun), IReplayRunCommand, ReplayRunResult, ReplayRunCommandHandler, Program.cs DI, ReplayRunCommandHandlerTests.

---

### 문서 추가

- **simulation-engine-architecture.md**: Simulation 모듈 구조(Domain/Application/Workers), apply(event) 단일 경로, IEngineStateApplier 설명. documentation/backend/ README 목차에 반영.

---

### Phase UI-1 — 캔버스 기반 에셋·관계 편집 (기초)

- **목표**: 목록/테이블 대신 캔버스에 에셋을 노드로, 관계를 엣지로 표시하고, 기본 CRUD와 관계 생성을 API와 연동.
- **계획·완료 내용**:
  - **의존성**: `@xyflow/react` (React Flow v12) 추가, 패키지 매니저 pnpm.
  - **라우트·네비**: `/canvas` 경로, AppLayout에 "캔버스" 링크 추가.
  - **캔버스 페이지**: `pages/canvas/` (AssetsCanvasPage, AssetNode). 마운트 시 getAssets + getRelationships 병렬 호출 → 노드(에셋)·엣지(관계) 구성. 노드 타입 `asset`, 커스텀 AssetNode(타입·메타 요약, Handle source/target).
  - **에셋 생성**: 툴바 "에셋 추가" → 모달(type, metadata) → createAsset → 새 노드 추가.
  - **에셋 수정**: 노드 클릭 → 사이드 패널(type, metadata) → updateAsset, connections는 엣지에서 유도.
  - **관계 생성**: 두 노드 선택 시 "관계 만들기" 활성화 → 다이얼로그(relationshipType, properties) → createRelationship → 새 엣지 추가. 동일 from→to 중복 시 버튼 비활성화.
  - **스타일**: AssetsCanvasPage.css, React Flow Controls/Background, fitView.
  - **테스트**: AssetsCanvasPage.test.tsx(로드 후 노드·버튼 노출, 로딩 표시). test/setup.ts에 ResizeObserver 목 추가.
- **산출물**: package.json(@xyflow/react), app/routes.tsx·AppLayout.tsx, pages/canvas/(index, ui/AssetsCanvasPage, AssetNode, CSS, test), 문서 [canvas-page-and-asset-node-ui.md](../frontend/canvas-page-and-asset-node-ui.md).

---

## 3. 참고 문서

- [temp.phases.md](../../temp.phases.md) — Phase 6 이상 단계별 로드맵·완료 기준
- [temp.uiux.integration.phases.md](../../temp.uiux.integration.phases.md) — UI/UX 통합 Phase (UI-1 완료, UI-2~UI-7 정의)
- [simulation-engine-tick-rules.md](../backend/simulation-engine-tick-rules.md) — 에셋 tick 스키마·due 엔진 규칙
- [simulation-api.md](../backend/simulation-api.md) — 시뮬레이션 API·Run·전파 진입점
- [simulation-engine-architecture.md](../backend/simulation-engine-architecture.md) — Simulation 모듈 구조, apply(event), IEngineStateApplier (Phase 7a)
- [event-types.md](../shared/event-types.md) — 이벤트 타입 Command/Observation 분류, 시뮬 vs Live 계약
- [2026-02-20-development-plan.md](2026-02-20-development-plan.md) — 이전 개발 계획

---
