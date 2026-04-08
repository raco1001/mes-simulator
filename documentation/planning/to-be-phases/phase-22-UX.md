# Phase 22 — 캔버스·시뮬 UX (레이아웃 지속성 / 다중 트리거 / 시뮬 시각화)

**캔버스에서 에셋 노드 배치를 유지하고, 시뮬레이션 트리거를 단일 선택에서 “트리거 가능한 노드 다중 선택”으로 확장하며, 시뮬 실행 중·전후의 시각적 피드백(간선 애니메이션·ByteByteGo 스타일 색)을 정리하는 UX Phase다.**  
백엔드 시뮬 엔진(Phase 12·13·21)은 유지하되, 프론트·계약·필요 시 최소 API 확장으로 사용자 작업 흐름을 개선한다.

---

## 1. 목표

### A. 가벼운 작업 — 노드 위치 지속성

- 새로고침·재방문 후에도 **에셋 노드가 마지막으로 배치된 좌표**에 가깝게 복원된다.
- 구현은 **React Flow 노드 `position`** 과 **에셋 `metadata` 관례 키** 조합으로 충분한 규모를 목표로 한다(별도 레이아웃 서버 불필요).

### B. 무거운 작업 — 트리거 속성·다중 트리거 시뮬

- 에셋(또는 ObjectType)에 **“트리거로 쓸 수 있는 속성”**을 분류할 수 있다.
- 시뮬레이션 패널에서 **트리거 후보 에셋을 체크박스 목록**으로 보여 주고, **전체 선택/해제·개별 토글**이 가능하다.
- **선택된 모든 에셋**이 (정의된 규칙에 따라) 시뮬 **트리거**로 반영된다.  
  현재 API는 `triggerAssetId` 단일 필드이므로, **의미 모델**(순차 다중 런 vs 단일 런·다중 시드)을 먼저 고정한 뒤 OpenAPI·백엔드·프론트를 순서 있게 확장한다.

### C. 시뮬 시각화 — 간선 애니메이션·팔레트·상태 전이

- **시뮬 실행 중** React Flow **간선**에 **점선 흐름(dash offset) 애니메이션**을 적용해 “데이터/상태가 흐른다”는 느낌을 준다. (`@xyflow/react` 커스텀 Edge 또는 `animated`·`strokeDasharray` + CSS/`requestAnimationFrame` 조합.)
- **ByteByteGo 다이어그램 느낌**의 색 팔레트: 다크 베이스, **은은한 보더**, 상태별 틴트(아래 표).
- **상태 전이 규칙**(노드·간선 스타일 공통 기준으로 정의):

| 상황 | 시각 (목표) |
| --- | --- |
| 시뮬 **시작 전** (기본·운영 캔버스) | **밝은 회색 계열** (노드/엣지 neutral idle) |
| 시뮬 **실행 중** — 에셋 **정상** | **초록 계열** 틴트 (기존 `liveStatus`·SSE와 연동) |
| 시뮬 **실행 중** — **비정상** | **붉은 계열** 틴트 |
| **중지 직후** (같은 탭·새로고침 없음) | **마지막 시뮬 결과 유지** — 노드 `liveProperties` / `liveStatus` 캐시를 지우지 않음, 간선 애니메이션만 OFF |
| **중지 후 새로고침** | **시작 전과 동일** idle 회색 (메모리 캐시 없음 → 라이브 오버레이 초기화) |
| **시뮬 중 새로고침** | Phase 21의 **`getRunningSimulationRuns` 동기화** + SSE로 **실행 중 상태 복구** — 간선 애니메이션·색상도 running 모드로 |

---

## 2. 제약

- `shared/api-schemas/openapi.json` 및 (필요 시) `shared/ontology-schemas/` 변경 시 **백엔드·프론트·파이프라인** 정합을 유지한다.
- **Clarity over complexity**: 위치 저장은 `metadata` 키 1~2개와 debounced 저장으로 시작한다. 다중 트리거는 **가장 단순한 시맨틱**(예: 선택된 각 에셋에 대해 동일 패치로 연속 `Run` 호출)부터 검토하고, 단일 Run 다중 시드가 필요하면 그때 핸들러 확장을 문서화한다.
- 기존 단일 트리거 UX는 **하위 호환**(기본값·마이그레이션 경로)을 유지한다.
- 트랙 C는 **프론트만**으로 달성 가능한 범위를 우선한다. “중지 직후 캐시”는 **세션 메모리(React state)** 로 충분하며, 새로고침 후 마지막 시뮬 스냅샷을 DB에 넣는 것은 Phase 22 범위 밖(필요 시 후속).

---

## 3. 선행 조건

- 캔버스: [`CanvasPage.tsx`](../../../servers/frontend/src/pages/canvas/ui/CanvasPage.tsx), `useNodesState`, 에셋 로드 시 그리드 배치 로직 존재.
- 시뮬: [`RunSimulationOnPanel.tsx`](../../../servers/frontend/src/features/run-simulation-on-panel/ui/RunSimulationOnPanel.tsx), `RunSimulationRequest`의 `triggerAssetId` 단일 필드.
- 메타데이터: `metadata.assetName` 등 well-known 키 패턴([`canvasMetadata.ts`](../../../servers/frontend/src/shared/lib/canvasMetadata.ts))과 동일하게 **예약 키**를 정하면 flat 편집 섹션과 충돌하지 않게 한다.
- 트랙 C: [`CanvasPage`](../../../servers/frontend/src/pages/canvas/ui/CanvasPage.tsx)의 `edges`·[`canvasConstants`](../../../servers/frontend/src/pages/canvas/lib/canvasConstants.ts), 노드 `data.liveStatus` / SSE 업데이트 경로, [`AssetNode.css`](../../../servers/frontend/src/pages/canvas/ui/AssetNode.css) 기존 상태 클래스.

---

## 4. 트랙 A — 캔버스 노드 좌표 저장 (단계별)

| 단계 | 작업 | 산출물 / 메모 |
| --- | --- | --- |
| A1 | **관례 확정**: 예) `metadata.canvasPosition = { x: number, y: number }` (또는 `flowPosition`). `isHiddenFromFlatMetadataKeys`에 추가해 “추가 메타데이터” raw JSON 노출 방지. | [`canvasMetadata.ts`](../../../servers/frontend/src/shared/lib/canvasMetadata.ts), [`assets.json`](../../../shared/api-schemas/assets.json) metadata 설명 한 줄 |
| A2 | **로드 병합**: `getAssets` 후 노드 생성 시 `metadata`에 좌표가 있으면 사용, 없으면 기존 `GRID_X`/`GRID_Y` 스태거. | [`CanvasPage.tsx`](../../../servers/frontend/src/pages/canvas/ui/CanvasPage.tsx) 또는 전용 `buildFlowNodesFromAssets` 유틸 |
| A3 | **드래그/이동 반영**: `onNodesChange`(또는 `onNodeDragStop`)에서 변경된 노드의 `position`을 로컬 state에 반영. | React Flow 이벤트 선택(드래그 종료만 저장하면 API 호출 수 감소) |
| A4 | **저장**: `updateAsset`로 해당 에셋 `metadata`만 병합 PATCH. **debounce**(300–800ms) 또는 drag-stop 시 1회 저장으로 스팸 방지. | `entities/asset` API, 에러 시 토스트/로그 |
| A5 | **신규 에셋**: 생성 직후 캔버스에 놓인 초기 좌표를 첫 저장 시 포함하거나, 빈 값이면 다음 이동 시 저장. | [`AddAssetModal`](../../../servers/frontend/src/pages/canvas/ui/AddAssetModal.tsx) 흐름과 정합 |
| A6 | **테스트**: Vitest로 위치 병합·예약 키 제외 유틸 단위 테스트; 필요 시 CanvasPage 통합 스모크. | `*.test.ts` |

**완료 기준 (A)**  
- 새로고침 후 노드가 이전과 동일(또는 근접) 좌표에 나타난다.  
- `canvasPosition`이 flat 메타 편집에 원시 blob으로 뜨지 않는다.

---

## 5. 트랙 C — 시뮬 시각화 (간선 애니메이션·팔레트·상태) (단계별)

**배치**: 트랙 A로 **노드 좌표·로드 경로**가 안정된 뒤 진행하는 것이 자연스럽다. 트랙 B와는 **병렬** 가능하나, **시뮬 패널 ↔ 캔버스 간 “실행 중” 플래그 공유**가 필요하므로 Phase 21 동기화(`getRunningSimulationRuns`)가 이미 있다면 **A2–A4 직후·B2 이전 또는 B2와 함께** 묶어도 된다.

| 단계 | 작업 | 산출물 / 메모 |
| --- | --- | --- |
| C1 | **캔버스 시뮬 phase** 단일 소스: `idle` \| `running` \| `stoppedCached` (가칭). `running` = 연속/단발 시뮬이 실제로 돌아가는 구간(SSE·running API true). `stoppedCached` = 사용자 **중지 직후** — 노드 `liveProperties` / `liveStatus`는 **유지**, 간선 애니만 끔. `idle` = **새로고침 후** 또는 한 번도 시뮬 안 함 — 라이브 오버레이 없음, **밝은 회색** idle 스타일. | React context 또는 `CanvasPage` state + `RunSimulationOnPanel`에서 set (lift state). |
| C2 | **ByteByteGo 스타일 토큰**: 다크 배경·은은한 보더. CSS 변수 예) `--canvas-surface`, `--canvas-edge-idle`, `--canvas-accent-ok`, `--canvas-accent-bad`, `--canvas-node-idle`. | `CanvasPage` / 전역 캔버스 테마, 기존 `data-canvas-theme`와 정합. |
| C3 | **노드 색**: idle에서 neutral gray 틴트; `running`에서만 `liveStatus` 기반 **초록/붉은** 틴트(기존 [`AssetNode`](../../../servers/frontend/src/pages/canvas/ui/AssetNode.tsx) 경로 확장). `stoppedCached`에서는 **마지막 live** 색 유지(애니만 없음). | `AssetNode.css`, `data`에 phase 전달 또는 셀렉터. |
| C4 | **간선 애니메이션**: `@xyflow/react` **커스텀 Edge** 또는 `defaultEdgeOptions` + `animated` 조합. 목표는 **점선이 흐르는** 느낌 → `strokeDasharray` + CSS `@keyframes` `stroke-dashoffset` 또는 RF 문서의 animated edge 패턴. **`running`일 때만** 활성. | [`canvasConstants`](../../../servers/frontend/src/pages/canvas/lib/canvasConstants.ts) `EDGE_TYPE`, edge 컴포넌트·CSS 한 벌. |
| C5 | **중지 버튼**: phase → `stoppedCached`, SSE 정리, **노드 live 데이터는 클리어하지 않음**. | 패널 핸들러와 Canvas phase 동기. |
| C6 | **페이지 마운트**: `getRunningSimulationRuns`가 true면 즉시 `running` + SSE 재구독(Phase 21) → 간선 애니·색상 running 모드. false면 `idle` → 회색, live 필드 초기화. | 기존 마운트 이펙트에 phase 초기화 한 줄 추가. |
| C7 | **테스트**: phase 전이 단위 테스트; 필요 시 스토리/스냅샷으로 idle vs running 엣지 클래스. | Vitest |

**완료 기준 (C)**  
- 시뮬 **실행 중**에만 간선에 점선(또는 동등한) **흐름 애니메이션**이 보인다.  
- **시작 전·중지+새로고침**은 노드·엣지가 **밝은 회색 idle**에 가깝다.  
- **실행 중**은 정상/비정상이 **초록/붉은** 틴트로 구분된다(데이터는 기존 `liveStatus` 등).  
- **중지 직후**(새로고침 없음)는 **마지막 시뮬 스냅샷**이 노드에 남고, 애니만 꺼진다.  
- **시뮬 중 새로고침** 후에도 running 동기화 시 **애니·색이 running**으로 복구된다.

---

## 6. 트랙 B — 트리거 속성 분류·다중 트리거 (단계별)

### 6.1 제품·데이터 모델 (먼저 합의)

| 결정 사항 | 선택지 | 비고 |
| --- | --- | --- |
| “트리거 속성” 정의 위치 | ObjectType `PropertyDefinition` 확장 vs 에셋 `metadata.triggerKeys` vs 별도 플래그 배열 | 온톨로지와 맞추려면 스키마 확장이 장기적으로 유리; MVP는 메타데이터·스키마 optional 필드로 시작 가능 |
| 다중 트리거 시맨틱 | **S1** 선택된 각 에셋에 대해 **순차** `POST /runs`(또는 내부 루프) | 구현 단순, Run ID 여러 개·이벤트 순서는 UI에 표시 |
| | **S2** 단일 Run에 `triggerAssetIds[]` + 핸들러에서 다중 시드 전파 | API·엔진 변경 큼; 재현·Tick 의미 정리 필요 |

**권장 순서**: UI 목록 + 체크박스까지 먼저 만들고, 백엔드는 **S1**으로 동작 검증 → 필요 시 S2를 Phase 22 후속 또는 Phase 23으로 분리.

### 6.2 구현 단계

| 단계 | 작업 | 산출물 / 메모 |
| --- | --- | --- |
| B1 | **후보 판별 함수**: “이 에셋이 트리거 후보인가?” — MVP는 `ObjectTypeSchema`의 속성 중 `simulationBehavior === 'Settable'` 등 기존 필드 조합, 또는 스키마에 `isTriggerCandidate` 같은 optional 추가. | `shared/lib` 또는 `entities/object-type-schema` |
| B2 | **시뮬 패널 UI**: 트리거 후보 에셋 테이블/리스트, 기본 전체 체크, 헤더 **전체 선택/해제**, 행별 체크박스. 검색/필터는 후순위. | [`RunSimulationOnPanel.tsx`](../../../servers/frontend/src/features/run-simulation-on-panel/ui/RunSimulationOnPanel.tsx) + CSS |
| B3 | **단발 실행**: 선택된 id들에 대해 S1이면 `runSimulation`을 순차 호출(또는 `Promise.all`이면 동시성·부하 주의 — 문서화). | `simulationApi.ts`, 에러 시 부분 실패 표시 |
| B4 | **지속 실행**: 현재는 단일 `triggerAssetId`로 Run 생성. **정책 선택**: (a) 첫 번째만 엔진 트리거로 유지 + 나머지는 무시(비추천), (b) 다중을 지원하도록 `StartContinuousRun` 요청 확장, (c) 지속 모드에서는 단일 선택만 허용하고 다중은 단발만. | OpenAPI·[`StartContinuousRunCommandHandler`](../../../servers/backend/DotnetEngine/Application/Simulation/Handlers/StartContinuousRunCommandHandler.cs) |
| B5 | **계약**: OpenAPI `RunSimulationRequest`에 `triggerAssetIds` optional 배열 추가 vs 별도 엔드포인트 — 팀 합의 후 단일 소스 반영. | `openapi.json`, C# DTO, TS 타입 |
| B6 | **테스트**: 패널에서 선택 수에 따른 API 호출 횟수·순서; 백엔드 단위 테스트는 S2 도입 시 확대. | Vitest / xUnit |

**완료 기준 (B)**  
- 트리거 후보가 아닌 에셋은 목록에서 제외되거나 비활성 처리된다.  
- 전체/개별 선택이 단발 시뮬에서 기대대로 동작한다.  
- 지속 실행 정책(B4)이 문서·UI에 명시되어 사용자가 오해하지 않는다.

---

## 7. 권장 작업 순서 (내일 이후)

1. **A1–A4** (위치 관례 + 로드/저장) — 리스크 낮고 체감 큼.  
2. **C1–C4** (시뮬 phase + 팔레트 + 노드 틴트 + 간선 애니) — React Flow 기능 위주, Phase 21 running 동기화와 맞물림.  
3. **C5–C6** (중지 캐시·마운트 idle/running) — 표의 상태 전이 완결.  
4. **B1–B3** (후보 판별 + 패널 UI + 단발 다중) — 백엔드 변경 최소.  
5. **B4–B5** (지속 실행·OpenAPI) — 제품 결정 후.  
6. **A6·B6·C7** 테스트 보강.

---

## 8. 리스크·메모

- **동시 `updateAsset`**: 노드 여러 개를 빠르게 옮기면 debounce로 마지막 위치만 저장될 수 있음 — 필요 시 “저장 중” 표시.  
- **다중 단발 Run**: 이벤트 로그가 길어짐 — 패널에 runId 목록 또는 요약 표시 고려.  
- **고아 Running 런**: Phase 21 이후에도 발생 가능 — 시뮬 패널의 `getRunningSimulationRuns` 동기화·중지 UX 유지.  
- **간선 애니메이션**: 엣지 수가 매우 많으면 CSS 애니 비용이 커질 수 있음 — `running`일 때만 클래스 부여, `prefers-reduced-motion` 존중 검토.

---

## 9. 완료 시 체크리스트

- [ ] A: 새로고침 후 캔버스 좌표 유지  
- [ ] A: `canvasPosition`(가칭)이 well-known 키로 flat 메타와 분리  
- [ ] C: 시뮬 **running** 시 간선 점선(흐름) 애니메이션  
- [ ] C: ByteByteGo 스타일 팔레트 + idle(밝은 회색) / running 정상·비정상(초록·붉은) / 중지 직후 캐시 / 중지+새로고침 idle / running+새로고침 복구  
- [ ] B: 트리거 후보 목록 + 체크박스 + 전체 선택/해제  
- [ ] B: 단발 시뮬에서 선택된 모든(또는 정의된) 트리거 반영  
- [ ] B: 지속 실행 정책 문서화 및 구현 일치  
- [ ] 공유 계약·타입 동기화 (openapi / 선택적 ontology)
