Phase 17 — Phase 3 (프론트엔드 최소 개선) 구현 계획

전제 / 코드베이스와의 차이

ObjectType 속성 폼은 문서의 settings/가 아니라 [servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx) 내 ObjectTypePanel의 ownProperties 행(약 757–815행)에만 존재하며, 현재 unit 입력이 없음. Task 3-B는 행에 UnitSelect를 추가하는 것으로 해석(교체가 아님).

SSE 페이로드: [simulationStream.ts](servers/frontend/src/entities/simulation/api/simulationStream.ts)의 SimulationTickEvent는 assetId, properties, status 등을 최상위에 둠. 문서의 event.payload?.properties가 아니라 event.properties 로 liveStates를 갱신해야 함.

캔버스의 [SimulationPanel](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx)은 이미 subscribeSimulationEvents로 노드 liveProperties를 갱신함. Task 3-D의 “시뮬레이션 페이지”는 라우트 [SimulationPage.tsx](servers/frontend/src/pages/simulation/ui/SimulationPage.tsx) 에 동일 패턴을 추가하는 작업.

Task 3-A — UnitSelect

신규: [servers/frontend/src/shared/ui/UnitSelect.tsx](servers/frontend/src/shared/ui/UnitSelect.tsx)

Phase 스니펫을 프로젝트 규칙에 맞게 조정: import type { CSSProperties } from 'react' 사용, React.CSSProperties 직접 참조 제거(verbatimSyntax/strict).

스타일은 문서의 인라인 다크 테마를 유지하되, 필요 시 [SimulationPage.css](servers/frontend/src/pages/simulation/ui/SimulationPage.css) 등과 톤만 맞춤.

Task 3-B — ObjectType 속성 행에 단위

파일: [AssetsCanvasPage.tsx](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx) (ObjectTypePanel 내부)

import { UnitSelect } from '@/shared/ui/UnitSelect'

각 ownProperties 행에서 dataType === 'Number'일 때만 UnitSelect 표시(온톨로지에서 unit이 Number 의미가 있을 때만 쓰는 것이 자연스러움). value={p.unit ?? ''}, onChange={(unit) => updatePropertyRow(i, { unit: unit || undefined })}

addPropertyRow 기본 객체는 그대로 두거나 unit 생략 유지.

Task 3-C — RelationshipsPage 매핑 UI

파일: [servers/frontend/src/pages/relationships/ui/RelationshipsPage.tsx](servers/frontend/src/pages/relationships/ui/RelationshipsPage.tsx)

타입: 이미 [entities/relationship](servers/frontend/src/entities/relationship/model/types.ts)에 PropertyMapping이 있으면 재사용; 로컬 interface 중복은 피함.

State: mappings 배열, fromSchema / toSchema를 ObjectTypeSchemaDto | null로 유지.

스키마 로드: fetch 대신 [getObjectTypeSchema](servers/frontend/src/entities/object-type-schema/api/objectTypeSchemaApi.ts)(또는 entities/object-type-schema public API)로 from/to asset의 type 기준 조회. useEffect 2개(문서와 동일 의존성).

UI: Properties textarea 아래에 매핑 행(소스/타겟 select, transform input, 제거 버튼, 행 추가). 단위 불일치 시 문서의 이모지 대신 title + 짧은 텍스트(warn) 또는 CSS 클래스로 표시(저장소 규칙과 정합).

생성/수정: createRelationship / updateRelationship body에 mappings 포함. 빈 fromProperty/toProperty 행은 제출 전 필터링.

openEdit: editing.mappings을 state에 반영; 목록 테이블에 mappings 요약 컬럼 1개 추가(선택, 짧게 n mappings).

Task 3-D — SimulationPage 실시간 상태

파일: [SimulationPage.tsx](servers/frontend/src/pages/simulation/ui/SimulationPage.tsx)

import { subscribeSimulationEvents } from '@/entities/simulation'

useState<Record<string, Record<string, unknown>>>({}) 로 liveStates 보관.

useEffect: subscribeSimulationEvents 등록, tickEvent.assetId와 **tickEvent.properties**로 병합 업데이트; cleanup에서 unsubscribe.

UI: 이벤트/결과 섹션 위에 “실시간 Asset 상태” 카드 그리드(문서 레이아웃). assets.find로 표시 이름 보조.

지속 실행 중에만 SSE가 의미 있으면, continuousRunId가 있을 때만 구독하도록 제한(불필요한 상시 연결 방지). 1회 실행 후에도 틱을 보려면 백엔드 스트림 정책에 맞춰 조정 — 기본은 continuousRunId != null일 때만 구독하는 것을 권장.

Task 3-D (추가) — AssetNode 동적 속성

파일: [servers/frontend/src/pages/canvas/ui/AssetNode.tsx](servers/frontend/src/pages/canvas/ui/AssetNode.tsx)

liveProperties가 있으면 Object.entries(liveProperties).map으로 key: value 나열, 값 없음은 —.

liveProperties가 없으면 기존 metadataSummary 폴백 유지해 레이아웃 회귀 최소화.

검증

npm run build 또는 npm test(프로젝트에 시뮬/관계 페이지 테스트가 없으면 최소 vitest 관련 스모크만).

수동: Relationships 생성 시 Network 페이로드에 mappings 포함, Simulation 지속 실행 시 SimulationPage 카드 갱신.

범위 밖

[CreateRelationshipPanel](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx)(캔버스 위저드)에 매핑 UI 추가는 Phase 3 문서에 없으므로 이번 PR에서 제외(후속 가능).
