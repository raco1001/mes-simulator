# 시뮬레이션 연동 (Phase 3)

프론트엔드에서 시뮬레이션 실행 버튼을 통해 백엔드 API를 호출하고 결과를 표시하는 구현 내용입니다.

---

## 개요

- **위치**: `AssetsPage.tsx` 내 시뮬레이션 섹션, `entities/simulation`
- **API**: `POST /api/simulation/runs` (RunSimulationRequest → RunResult)
- **역할**: 사용자가 "시뮬레이션 실행" 클릭 시 트리거 에셋(선택 시) 또는 기본값으로 런 실행, runId·메시지 표시

---

## UI

- **버튼**: "시뮬레이션 실행" — 클릭 시 `runSimulation` 호출
- **로딩**: simulationLoading 상태로 버튼 비활성화 또는 스피너 표시
- **결과**: simulationResult(runId, message) 성공 시 표시, simulationError 실패 시 표시
- (선택) 트리거 에셋 ID, maxDepth 등 입력 필드 확장 가능

---

## Entity

- **타입**: `RunSimulationRequest` (triggerAssetId, patch?, maxDepth?), `RunResult` (runId, message 등) — `entities/simulation/model/types.ts`
- **API**: `runSimulation(request)` — `entities/simulation/api/simulationApi.ts`, `POST /api/simulation/runs`
- **노출**: `entities/simulation/index.ts`에서 export

---

## HTTP 클라이언트

- `shared/api/httpClient` 사용. baseURL·헤더는 기존 Asset/State API와 동일(환경 변수 또는 설정에서 로드)

---

## 참고

- Backend [simulation-api.md](../backend/simulation-api.md) — 요청/응답 스키마, RunResult 구조
