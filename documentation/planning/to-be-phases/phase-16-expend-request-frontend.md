Phase 16 — 클라이언트 기능 확장 (ObjectType CRUD, Asset 삭제, 관계 속성 확장)

목표: 클라이언트 화면에서 ObjectTypeSchema 생성/수정/삭제, Asset 생성 시 ObjectType 선택, Asset 삭제, 관계 설정 시 asset metadata 속성까지 선택할 수 있도록 한다.

---

## Step 1: 백엔드 DELETE 엔드포인트 추가

**목표**: ObjectTypeSchema와 Asset 각각에 DELETE를 추가한다. 두 도메인 모두 동일한 hexagonal 패턴을 따르므로 구현이 단순하다.

### 1-1. ObjectTypeSchema DELETE

신규 파일:

- `Application/ObjectType/Ports/Driving/IDeleteObjectTypeSchemaCommand.cs`

```csharp
public interface IDeleteObjectTypeSchemaCommand
{
    Task<bool> DeleteAsync(string objectType, CancellationToken cancellationToken = default);
}
```

- `Application/ObjectType/Handlers/DeleteObjectTypeSchemaCommandHandler.cs` — repository에 위임

수정 파일:

- `Application/ObjectType/Ports/Driven/IObjectTypeSchemaRepository.cs` — `DeleteAsync(string objectType)` 추가
- `Infrastructure/Mongo/MongoObjectTypeSchemaRepository.cs` — `DeleteOneAsync` 구현
- `Presentation/Controllers/ObjectTypeSchemaController.cs` — `DELETE /api/object-type-schemas/{objectType}` 추가 (204 / 404)
- `Program.cs` — DI 등록

### 1-2. Asset DELETE

동일 패턴:

- `IDeleteAssetCommand`, `DeleteAssetCommandHandler`
- `IAssetRepository.DeleteAsync`, `MongoAssetRepository` 구현
- `AssetController` — `DELETE /api/assets/{id}` (204 / 404)
- `Program.cs` DI 등록

### Why this fits current scale

단순 repository 위임이며, 기존 GET/POST/PUT 패턴 복제. 추가 인프라 불필요.

### If scale increases

관계가 걸린 Asset 삭제 시 연관 Relationship 자동 cascade 삭제 또는 참조 무결성 검사 추가 가능.

---

## Step 2: 프론트엔드 API 레이어 확장

**목표**: Step 1에서 추가한 DELETE 엔드포인트를 프론트엔드 API 레이어에 반영한다.

수정 파일:

- `entities/object-type-schema/api/objectTypeSchemaApi.ts`

```ts
export async function deleteObjectTypeSchema(objectType: string): Promise<void> {
  await fetch(`${API_BASE_URL}/api/object-type-schemas/${objectType}`, { method: 'DELETE' })
}
```

- `entities/object-type-schema/index.ts` — `deleteObjectTypeSchema` re-export 추가

- `entities/asset/api/assetApi.ts`

```ts
export async function deleteAsset(id: string): Promise<void> {
  await fetch(`${API_BASE_URL}/api/assets/${id}`, { method: 'DELETE' })
}
```

- `entities/asset/index.ts` — `deleteAsset` re-export 추가

### Why this fits current scale

API 레이어를 먼저 분리해두면 UI 변경과 독립적으로 테스트 가능. fetch 한 줄 추가이므로 복잡도 없음.

---

## Step 3: ObjectType 관리 패널 (홈 캔버스 사이드 패널)

**목표**: 기존 relPanel / assetPanel / simPanel과 동일한 사이드 패널 패턴으로 ObjectTypeSchema를 CRUD할 수 있는 `ObjectTypePanel`을 추가한다.

UX 구조:

```
[툴바] ... | [관계 만들기] | [ObjectType 관리] | [시뮬레이션]
                                  ↓ 클릭
                      ┌──────────────────────┐
                      │ ObjectType 목록       │
                      │  • freezer  [수정][삭제] │
                      │  • battery  [수정][삭제] │
                      │  [+ 새 ObjectType]    │
                      ├──────────────────────┤
                      │ 생성/수정 폼           │
                      │  objectType (string)  │
                      │  displayName          │
                      │  traits (3 select)    │
                      │  ownProperties 목록    │
                      │    key / dataType /   │
                      │    simulationBehavior /│
                      │    mutability / unit  │
                      │    [+ 속성 추가]       │
                      │  [저장]               │
                      └──────────────────────┘
```

수정 파일:

- `AssetsCanvasPage.tsx`
  - `objectTypePanelOpen` state 추가
  - 툴바에 "ObjectType 관리" 버튼 추가 (relMode 등과 상호 배타)
  - `ObjectTypePanel` 컴포넌트 인라인 구현: 목록 + 생성/수정 폼
  - `createObjectTypeSchema`, `updateObjectTypeSchema`, `deleteObjectTypeSchema` 호출
  - 저장/삭제 후 `objectTypeSchemas` 상태 갱신

- `AssetsCanvasPage.css` — ObjectTypePanel 스타일 추가

### Why this fits current scale

새 route 없이 기존 홈 캔버스에서 모든 온톨로지 관리가 가능. 기존 패널 패턴 재사용으로 CSS/UX 일관성 유지.

### If scale increases

ObjectType 수가 많아지면 패널 내 검색/필터 추가. AbstractSchema 구분 표시(인터페이스 vs 구체 스키마).

---

## Step 4: Asset 생성/수정 — ObjectType 선택 강화

**목표**: Asset 생성/수정 시 type 필드를 ObjectTypeSchema 목록에서 선택하도록 개선한다. ObjectType 선택 시 `ownProperties` 기본값이 자동 반영되고, 사용자가 임의 속성을 추가할 수 있다.

현재 상태:
- type 필드: 자유 텍스트 + datalist(제안)
- Schema Defaults: 선택된 type과 일치하는 스키마가 있으면 read-only 표시

개선 내용:

수정 파일:

- `AssetsCanvasPage.tsx` — `AddAssetModal` 및 에셋 편집 `SidePanel`

  1. type 필드를 `<select>` (ObjectTypeSchema 목록)로 변경. 빈 값(`-- 선택 --`)도 허용.
  2. ObjectType 선택 시 해당 schema의 `resolvedProperties ?? ownProperties`를 metadata 초기값으로 자동 주입 (`baseValue` 사용).
  3. 자동 주입된 속성은 `Immutable`이면 readOnly, `Mutable`이면 편집 가능.
  4. 스키마에 없는 임의 속성 추가(기존 Metadata 영역)는 그대로 유지.

```ts
// ObjectType 선택 시
const schema = objectTypeSchemas.find(s => s.objectType === selectedType)
const schemaProps = schema?.resolvedProperties ?? schema?.ownProperties ?? []
const initialMetadata = Object.fromEntries(
  schemaProps.map(p => [p.key, p.baseValue ?? ''])
)
```

### Why this fits current scale

select UI가 datalist보다 명확하고 유효성이 보장됨. 자동 주입은 로컬 state만 사용.

### If scale increases

ObjectType 수가 많아지면 검색 가능한 combobox로 교체. abstractSchema: true인 스키마는 선택 목록에서 제외.

---

## Step 5: Asset 삭제 UI

**목표**: 에셋 편집 사이드 패널에 삭제 버튼을 추가한다. 실수 방지를 위해 확인 단계를 포함한다.

수정 파일:

- `AssetsCanvasPage.tsx` — 에셋 편집 `SidePanel`
  - "에셋 삭제" 버튼 추가 (패널 하단, destructive 스타일)
  - 클릭 시 confirm 또는 인라인 확인 UI (`"정말 삭제하시겠습니까? [취소] [삭제]"`)
  - 확인 후: `deleteAsset(id)` 호출 → 패널 닫기 → 노드/엣지 상태에서 해당 asset 제거

```ts
const handleDeleteAsset = async (id: string) => {
  await deleteAsset(id)
  setNodes(prev => prev.filter(n => n.id !== id))
  setEdges(prev => prev.filter(e => e.source !== id && e.target !== id))
  setSelectedAssetId(null)
}
```

- `AssetsCanvasPage.css` — 삭제 버튼 destructive 스타일

### Why this fits current scale

단일 API 호출 + 로컬 state 갱신. 낙관적 업데이트(즉시 UI 반영 후 에러 처리)로 UX 개선 가능.

### If scale increases

삭제 시 연결된 Relationship을 함께 삭제할지 cascade 여부를 선택하는 옵션 추가.

---

## Step 6: 관계 설정 — Asset metadata 속성도 선택 가능

**목표**: 관계 설정 Step 3(속성 설정)에서 source asset의 `metadata` 키를 ObjectType 스키마 속성과 함께 선택지로 제공한다. 스키마 속성은 타입/동작 정보가 있고, metadata 속성은 실제 저장된 값 기준으로 제공된다.

현재 상태:
- `eligibleProps = (sourceSchema?.resolvedProperties ?? sourceSchema?.ownProperties ?? []).filter(isEligibleProperty)`
- asset의 `metadata` 키는 표시되지 않음

개선 내용:

수정 파일:

- `AssetsCanvasPage.tsx`

```ts
// metadata 기반 추가 속성 (스키마에 없는 키만)
const metadataEligibleProps = useMemo(() => {
  if (!sourceAsset) return []
  const schemaKeys = new Set(eligibleProps.map(p => p.key))
  return Object.keys(sourceAsset.metadata)
    .filter(key => !schemaKeys.has(key))
    .map(key => ({ key, source: 'metadata' as const }))
}, [sourceAsset, eligibleProps])
```

  - `CreateRelationshipPanel`의 transfers 체크박스 목록에 두 그룹으로 구분 표시:
    - `[스키마 속성]`: 기존 `eligibleProps` (dataType, simulationBehavior 표시)
    - `[에셋 속성]`: `metadataEligibleProps` (키 이름 + 현재 값 표시)
  - 두 그룹 모두 `transfers` 배열에 `{ key, ratio }` 형태로 포함

### Why this fits current scale

로컬 state 계산만 추가. 백엔드 API 변경 없음.

### If scale increases

metadata 속성에도 단위 정보를 선택할 수 있는 옵션 추가. 타입 추론(숫자 값인 키만 표시)으로 필터링 강화.
