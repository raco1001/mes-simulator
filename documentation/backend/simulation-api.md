# Simulation API (시뮬레이션 실행)

시뮬레이션 런 1회 실행을 트리거하고 runId를 반환하는 API 구현 내용입니다. Phase 3 및 이후 전파·이벤트 확장의 진입점입니다.

---

## 개요

- **경로**: `api/simulation`
- **역할**: 트리거 에셋 + 옵션(패치, maxDepth)으로 시뮬레이션 런 1회 실행, runId 및 결과 반환
- **저장소**: MongoDB `factory_mes.simulation_runs`, `factory_mes.events` (전파 시 이벤트 기록)

---

## API 엔드포인트

| 메서드 | 경로 | 설명 |
|--------|------|------|
| POST | `/api/simulation/runs` | 시뮬레이션 런 1회 실행. Body: triggerAssetId(필수), patch(선택), maxDepth(선택). 응답: 201 + RunResult(runId, message 등) |

- **RunSimulationRequest**: TriggerAssetId, Patch(상태 패치), MaxDepth
- **RunResult**: RunId, Message 등 (에셋·관계 개수, 전파 결과 요약 등)

---

## Application

- **Driving Port**: `IRunSimulationCommand` — RunAsync(RunSimulationRequest, CancellationToken) → RunResult
- **Handler**: `RunSimulationCommandHandler` — 에셋·관계 조회, SimulationRun 생성·저장, BFS 전파(구현 수준에 따라 1회 전파 또는 뼈대만), 이벤트 기록
- **Driven Ports**: IAssetRepository, IRelationshipRepository, ISimulationRunRepository, IEventRepository

---

## Infrastructure

- **SimulationRun**: `MongoSimulationRunDocument`, `MongoSimulationRunRepository` — 컬렉션 `simulation_runs`
- **Event**: `MongoEventDocument`, `MongoEventRepository` — 컬렉션 `events` (simulationRunId, relationshipId 등 선택 필드 지원)

---

## Presentation

- **Controller**: `SimulationController` — Route `api/simulation`, POST("runs") → CreateRun(RunSimulationRequest), triggerAssetId 비어 있으면 400

---

## 참고

- 시뮬레이션 전파 규칙·이벤트 Kafka 발행 등은 Phase 4 이후 문서에서 다룸.
- [temp.phases.md](../../temp.phases.md) 에 Phase 3.5·4 작업 목록 정리됨.
