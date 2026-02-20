# 에셋 페이지 및 메타데이터 UI

에셋 목록·생성·수정 화면과 메타데이터 키-값 입력 UI 구현 내용입니다. Phase 2 반영.

---

## 개요

- **페이지**: `src/pages/assets/ui/AssetsPage.tsx`
- **역할**: 에셋 목록 표시, 에셋 생성(타입·연결·메타데이터), 에셋 수정(모달), 시뮬레이션 실행 버튼
- **스타일**: `AssetsPage.css` (테이블, 폼, 메타데이터 행, 수정 모달, 다크 모드)

---

## 에셋 목록

- 테이블 컬럼: ID, Type, Connections, Metadata, Created, (수정 버튼)
- Metadata 열: `metadataSummary(asset.metadata)` — 최대 2개 키를 `key: value` 형태로 표시, 없으면 "-"
- 수정 버튼 클릭 시 해당 에셋으로 수정 폼 오픈(인라인 섹션)

---

## 에셋 생성 폼

- **Type (필수)**: 단일 텍스트 입력
- **Connections**: 쉼표 구분 문자열 → 배열로 전달
- **Metadata (선택)**: 키-값 쌍 목록
  - 각 행: key 입력, value 입력, "삭제" 버튼
  - "항목 추가"로 행 추가
  - 제출 시 빈 키(trim 후)인 행은 제외하고 `Record<string, unknown>`으로 변환해 CreateAssetRequest.metadata에 포함
- 제출: `createAsset({ type, connections, metadata })` 호출 후 목록 재조회, 폼 초기화

---

## 에셋 수정

- **오픈**: 목록에서 "수정" 클릭 → `editingAsset` 설정, `editType`, `editConnections`, `editMetadata`를 해당 에셋 값으로 채움
- **편집**: 생성 폼과 동일한 키-값 메타데이터 UI. Type, Connections 수정 가능
- **저장**: `updateAsset(editingAsset.id, { type, connections, metadata })` 호출. connections 필수이므로 폼에서 파싱한 배열 전달
- **취소**: `editingAsset` null로 모달 닫기

---

## 검증·에러

- 생성: Type 비어 있으면 "Type is required", createError 표시
- 수정/생성 API 실패 시 createError 또는 editError에 메시지 표시
- 메타데이터: 빈 키 행은 객체 변환 시 제외, 중복 키는 후행 값 우선

---

## 참고

- [entities-and-api-client.md](./entities-and-api-client.md) — getAssets, createAsset, updateAsset, 타입 정의
- [simulation-integration.md](./simulation-integration.md) — 시뮬레이션 실행 버튼 연동
