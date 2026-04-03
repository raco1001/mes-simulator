# Simulation 엔진 아키텍처

시뮬레이션 모듈의 레이어 구조, 도메인 타입, Workers, 그리고 상태 변경 단일 진입점(apply)에 대한 문서입니다. Live 전환 시 동일 엔진을 쓰기 위한 설계가 반영되어 있습니다.

---

## 모듈 구조 요약

| 레이어 | 경로 | 내용 |
|--------|------|------|
| **Domain** | `Domain/Simulation/` | EventKind, SimulationRunStatus (ValueObjects), EventTypes (Constants) |
| **Application** | `Application/Simulation/` | Ports(Driving/Driven), Handlers, Dto, Rules, **Workers** |
| **Infrastructure** | Mongo/Kafka | SimulationRunRepository, EventRepository, EventPublisher |
| **Presentation** | Controllers | SimulationController |

- **Workers**: 주기적 전파를 수행하는 `SimulationEngineService` (BackgroundService). `Application/Simulation/Workers/` 에 두어 요청 기반 Handlers와 구분.
- **이벤트 타입 분류**(Command/Observation) 및 시뮬/Live 계약은 [documentation/shared/event-types.md](../shared/event-types.md) 참고.

---

## Domain/Simulation

- **ValueObjects**: `EventKind` (Command / Observation), `SimulationRunStatus` (Pending, Running, Stopped, Completed).
- **Constants**: `EventTypes` — Observation 상수(`SimulationStateUpdated`, `PowerChanged`, `StateTransitioned`), Command 상수(`StartMachine`, `StopMachine`, `ChangeSpeed`), `GetKind(string? eventType)` 헬퍼.

엔티티/값 객체/상수는 Application·Infrastructure에 의존하지 않습니다.

---

## 상태 변경 단일 경로: apply(event)

tick/전파 경로에서 **에셋 상태가 바뀌는 곳은 모두 apply 경로만** 사용합니다. Live 교체 시 동일 엔진(apply)을 쓰기 위한 수렴입니다.

### IEngineStateApplier (Driven Port)

- **인터페이스**: `Application/Simulation/Ports/Driven/IEngineStateApplier.cs`
- **시그니처**: `Task ApplyAsync(EventDto evt, StateDto mergedState, CancellationToken cancellationToken)`
- **역할**: "이 병합된 상태로 저장 + 이 이벤트 저장·발행". UpsertState → Append(이벤트) → Publish 순서로 수행.

### EngineStateApplier (구현체)

- **위치**: `Application/Simulation/EngineStateApplier.cs`
- **동작**: `IAssetRepository.UpsertStateAsync(mergedState)` → `IEventRepository.AppendAsync(evt)` → `IEventPublisher.PublishAsync(evt)`.

### 전파 루프에서의 사용

- **RunSimulationCommandHandler.RunOnePropagationAsync**: BFS로 노드를 처리할 때, 기존의 "MergeState → UpsertState → 이벤트 Append/Publish"를 **이벤트 생성 → `_applier.ApplyAsync(nodeEvent, mergedState)`** 한 번 호출로 대체.
- **규칙(ContainsRule, SuppliesRule, ConnectedToRule)에서 나온 이벤트**: 대상 에셋 상태는 해당 노드를 나중에 dequeue할 때 apply로 갱신되므로, 규칙 이벤트는 Append/Publish만 수행(상태 쓰기는 apply 경로만 사용).

이를 통해 "상태 변경은 apply 경로만" 되며, Live 모드에서는 Observation을 주입한 뒤 같은 apply 경로를 타게 하거나, 필요 시 `IEngineStateApplier` 구현을 교체할 수 있습니다.

---

## SSE 확장 경로 (Future): Redis Pub/Sub

현재 프로젝트는 단일 인스턴스 포트폴리오 규모이므로 `SseAlertChannel`(in-memory fan-out)만으로 충분합니다. 이 단계에서는 Redis를 제거해 인프라 복잡도를 줄이는 것이 우선입니다.

다중 인스턴스 확장이 필요해지면 `IAlertNotifier` 포트는 유지하고 구현체만 `RedisBackedSseChannel`로 교체합니다.

- 각 backend 인스턴스는 Redis Pub/Sub 채널을 구독
- Alert 수신 시 인스턴스 로컬 SSE 클라이언트로 fan-out
- API 계약과 도메인 로직 변경 없이 수평 확장 가능

즉, 현재는 단순한 메모리 기반 구현을 사용하고, 확장 시점에 어댑터만 교체하는 전략을 채택합니다.

---

## 참고

- [simulation-api.md](simulation-api.md) — Simulation API 엔드포인트, RunResult.
- [simulation-engine-tick-rules.md](simulation-engine-tick-rules.md) — tick 주기, due 에셋, BackgroundService tick 루프.
- [event-types.md](../shared/event-types.md) — Command/Observation 분류, 시뮬레이터 vs Live 계약.
