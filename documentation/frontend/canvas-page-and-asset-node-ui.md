# 캔버스 페이지 및 에셋 노드 UI

캔버스 기반 에셋·관계 편집 화면 구현 내용입니다. Phase UI-1 반영.

---

## 개요

- **페이지**: `src/pages/canvas/ui/AssetsCanvasPage.tsx`
- **경로**: `/canvas` (네비 "캔버스" 링크)
- **역할**: 에셋을 노드로, 관계를 엣지로 표시. 에셋 생성·수정, 관계 생성이 API와 연동됨.
- **라이브러리**: `@xyflow/react` (React Flow v12). 스타일: `@xyflow/react/dist/style.css`, `AssetsCanvasPage.css`

---

## 캔버스 및 데이터 로드

- 마운트 시 `getAssets()` + `getRelationships()` 병렬 호출.
- **노드**: 에셋 1건당 1개. `id = asset.id`, `position = { x: index * 220, y: index * 140 }` (그리드), `data.asset = AssetDto`, `type = 'asset'`.
- **엣지**: 관계 1건당 1개. `id = rel.id`, `source = rel.fromAssetId`, `target = rel.toAssetId`. `fromAssetId`/`toAssetId`에 해당하는 노드가 없으면 해당 엣지는 그리지 않음.
- 로딩/에러 시 "로딩 중...", "에러: ..." 문구 표시.

---

## 커스텀 노드 (AssetNode)

- **컴포넌트**: `src/pages/canvas/ui/AssetNode.tsx`
- **표시**: `data.asset`의 `type`, metadata 요약(최대 2개 키 `key: value`, 없으면 "-").
- **Handle**: `Position.Left` target, `Position.Right` source (관계 연결용).
- React Flow `nodeTypes={{ asset: AssetNode }}`로 등록.

---

## 에셋 추가

- 툴바 **"에셋 추가"** 버튼 → 모달 오픈.
- **폼**: Type(필수), Metadata(키-값 행, "항목 추가"/삭제). connections는 빈 배열 고정.
- 제출: `createAsset({ type, connections: [], metadata })` 호출 후, 응답 에셋을 새 노드로 추가. 위치는 마지막 노드 오른쪽 또는 (0,0).

---

## 에셋 수정

- 노드 **클릭** 시 오른쪽 **사이드 패널** 표시.
- 패널: 선택 노드 기준 Type, Metadata(키-값 행) 편집 폼. 저장 시 `updateAsset(id, { type, connections, metadata })` 호출. `connections`는 현재 엣지에서 source=해당 노드인 target 목록으로 계산해 유지.
- 저장 후 노드 `data.asset` 갱신, 패널 닫기.

---

## 관계 만들기

- **두 노드 선택** 시(React Flow 다중 선택) 툴바 **"관계 만들기"** 버튼 활성화. 이미 동일 from→to 엣지가 있으면 비활성화.
- 클릭 시 다이얼로그: 관계 타입(필수, feeds_into / contains / located_in), Properties(JSON, 선택).
- From/To: 선택 순서대로 첫 번째 노드 → from, 두 번째 → to. 다이얼로그에 "From: {id} → To: {id}" 표시.
- 제출: `createRelationship({ fromAssetId, toAssetId, relationshipType, properties })` 호출 후, 응답 관계를 새 엣지로 추가.

---

## 스타일 및 UX

- `AssetsCanvasPage.css`: 툴바, 캔버스 영역, AssetNode 카드, 사이드 패널, 모달. CSS 변수 `--app-border`, `--app-bg`, `--app-muted`, `--app-error` 사용.
- React Flow: `Controls`, `Background`, `fitView`, `fitViewOptions={{ padding: 0.2 }}`.

---

## 테스트

- `src/pages/canvas/ui/AssetsCanvasPage.test.tsx`: getAssets/getRelationships mock, 로딩 후 "freezer" 노드 및 "에셋 추가"/"관계 만들기" 버튼 노출 검증. 로딩 문구 표시 후 사라짐 검증.
- `src/test/setup.ts`: jsdom 환경에서 `ResizeObserver` 미정의 시 목 구현 추가 (React Flow 의존).

---

## 참고

- [entities-and-api-client.md](./entities-and-api-client.md) — getAssets, createAsset, updateAsset
- [assets-page-and-metadata-ui.md](./assets-page-and-metadata-ui.md) — 메타데이터 키-값 UI 패턴
- [documentation/backend/relationship-api.md](../backend/relationship-api.md) — 관계 API
- [temp.uiux.integration.phases.md](../../temp.uiux.integration.phases.md) — Phase UI-1 정의 및 후속 Phase
