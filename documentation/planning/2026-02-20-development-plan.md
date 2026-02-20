# 개발 계획 (2026-02-20)

본 문서는 Factory MES를 점검한 결과와, 이에 따른 개발 계획을 정리한 것입니다.

---

## 1. 목표 정리

- **에셋(Asset)**: 공장 또는 회사의 자원을 일반화한 객체.
- **관계**: 에셋 간 관계의 **종류**, **방향 유무**, **관계에 붙는 속성**을 자유롭게 담을 수 있어야 함.
- **운영 시뮬레이션**: 위 모델을 바탕으로 운영 상황을 시뮬레이션할 수 있는 **기본 준비**가 되어 있어야 함.

---

## 2. 현재 상태 요약

(Phase 1·2·3 완료 시점 기준)

| 항목                 | 상태    | 비고                                                                              |
| -------------------- | ------- | --------------------------------------------------------------------------------- |
| 에셋 엔티티          | ✅      | id, type, connections, metadata, createdAt/updatedAt                              |
| 에셋 단위 메타데이터 | ✅      | API·도메인·DB 지원. UI에서 생성/수정 시 metadata 입력 가능 (Phase 2).             |
| 에셋 간 연결         | ✅+관계 | connections 유지. 관계(Relationship)를 별도 엔티티로 확장 완료 (Phase 1).         |
| 관계 종류            | ✅      | Relationship API: fromAssetId, toAssetId, relationshipType, properties            |
| 관계 방향            | ✅      | fromAssetId → toAssetId 로 방향 명시. (Direction enum은 확장 시 선택)             |
| 관계 단위 속성       | ✅      | properties(관계 단위 속성) 저장·조회 가능                                         |
| 시뮬레이션           | ✅ 뼈대 | POST /api/simulation/run 호출·에셋·관계 개수 반환. 전파·이벤트·run 세션은 미구현. |

**이미 준비된 것**

- 에셋 = 자원 일반화 객체. metadata로 설비/자원별 속성 확장 가능.
- 관계 = 1급 엔티티(Relationship). 타입·방향·속성 저장·조회 가능.
- 상태(states)·이벤트(events) 컬렉션으로 시뮬레이션/분석 기반 데이터 확보 가능.
- 시뮬레이션 "실행" 버튼 → 백엔드 API 호출·결과 표시까지 연동 완료.

---

## 3. 다음 단계에서 다룰 부분

Phase 1·2·3 완료로 관계 모델 확장(ADR-20260220, 별도 엔티티)과 에셋 메타데이터 UI, 시뮬레이션 기본 연동은 갖춰진 상태다. 이어서 아래를 진행한다.

- **상태 전파 시뮬레이션**: 트리거(에셋+상태 패치) 기준 그래프 순회(BFS), 규칙 기반 전파, 이벤트 로그·Kafka 발행.
- **Pipeline 연동**: 백엔드에서 발생시킨 시뮬레이션 이벤트를 Pipeline이 소비해 동작하도록 이벤트 규약 맞추기.
- **사용자 제어**: 시뮬레이션 시작/중단, 실행 결과(run 단위 이벤트) 조회·UI.

---

## 4. 단계별 개발 계획

### Phase 1: 관계 모델 확장 (데이터·API) — 완료

- **목표**: 관계의 종류·방향·관계 단위 속성을 저장·조회할 수 있게 함.
- **완료 기준**: API로 관계 타입·방향·속성을 생성·수정·조회할 수 있음. (ADR-20260220, Relationship 엔티티·API 반영)

### Phase 2: 에셋 메타데이터 UI — 완료

- **목표**: 에셋 생성·수정 시 메타데이터를 UI에서 입력·수정 가능하게 함.
- **완료 기준**: UI에서 metadata를 넣어 생성/수정한 에셋이 목록·상세에 반영됨.

### Phase 3: 시뮬레이션 기본 연동 — 완료

- **목표**: "시뮬레이션 실행" 버튼이 백엔드 시뮬레이션 API를 호출하고, 최소한의 시뮬레이션 동작이 이루어짐.
- **완료 기준**: UI에서 시뮬레이션 실행 시 백엔드가 그래프/에셋을 사용한 시뮬레이션을 수행하고 결과가 확인 가능함. (현재는 에셋·관계 개수 반환까지 구현된 뼈대 상태.)

---

### Phase 3.5: 관계 탐색 + 시뮬레이션 런 세션

- **목표**: 한 번의 "실행"을 세션(SimulationRun)으로 묶고, 트리거 기반 1회 전파까지 구현.
- **주요 작업**: IRelationshipRepository에 GetOutgoing(및 GetIncoming) 추가, simulation_runs 컬렉션·도메인, RunSimulation 유스케이스 BFS 전파, POST /api/simulation/runs (triggerAssetId, patch, maxDepth → runId).
- **완료 기준**: 트리거 에셋 + patch로 1회 전파 동작, runId로 실행 구분·events 기록.

### Phase 4: 규칙 기반 전파 + 파이프라인 이벤트

- **목표**: "상태 전파 시뮬레이션" 완성. Pipeline이 소비할 이벤트 규약 맞추기.
- **주요 작업**: IPropagationRule·PropagationResult 도입, 룰 2~3개(예: Supplies, Contains, Controls), RunSimulation에 룰 적용, 이벤트 Kafka 발행, GET /api/simulation/runs/{runId}/events.
- **완료 기준**: 트리거 → 룰 기반 전파 → 이벤트 DB 저장·Kafka 발행, Pipeline 소비 가능.

### Phase 5: 실행 결과 조회 + UI

- **목표**: 사용자가 실행 결과(무슨 일이 일어났는지)를 확인할 수 있게 함.
- **주요 작업**: 시뮬레이션 실행 버튼을 POST /api/simulation/runs와 연결, runId 수신, 이벤트 목록 화면(runId 필터). 필요 시 관계 생성/수정 전용 UI(타입·방향·메타데이터).
- **완료 기준**: UI에서 시뮬레이션 실행 후 runId로 이벤트 목록 조회 가능.

### Phase 6 (확장): 사용자 시작/중단·지속 이벤트

- **목표**: 백엔드가 시뮬레이션 이벤트를 계속 발생시키고, 사용자가 시작/중단 제어.
- **방향**: 우선 반복 실행(주기적 또는 "한 번 더" 호출)으로 시작/중단 구현. 필요 시 장시간 런(SimulationRun 상태, start/stop API)으로 확장.

---

상세 작업 목록·완료 기준·참고 파일은 [temp.phases.md](../../temp.phases.md) 에 정리되어 있으며, 후속 에이전트는 해당 문서를 기준으로 Phase 단위 작업을 진행하면 된다.

---

## 5. 참고 문서

- [ADR-20260218: ML 파이프라인 및 시뮬레이션 엔진 위치 결정](../ADR/ADR-20260218.md)
- [ADR-20260219: MVP 구현 완료 및 Docker Compose 통합](../ADR/ADR-20260219.md)
- [ADR-20260220: 에셋 관계 모델 확장](../ADR/ADR-20260220.md)
- [README](../../README.md) — 컨셉과 용도
- [infrastructure/mongo/MODEL.md](../../infrastructure/mongo/MODEL.md) — 현재 MongoDB 모델

---
