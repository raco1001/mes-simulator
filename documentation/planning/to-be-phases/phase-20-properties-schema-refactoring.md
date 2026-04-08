# 구현 계획: extraProperties 시뮬레이션 연산 전면 통합

## 목표

`metadata.extraProperties`를 ObjectType의 `ownProperties`와 동일한 연산 경로(시뮬레이터 → State → 파이프라인)로 처리한다.
인스턴스가 직접 정의한 확장 속성도 스키마 정의 속성과 동일한 `simulationBehavior` / `mutability` 규칙으로 시뮬레이션된다.

---

## 변경 범위 요약

| 레이어       | 변경 파일                                                        | 유형                                                              |
| ------------ | ---------------------------------------------------------------- | ----------------------------------------------------------------- |
| **Frontend** | `entities/asset/model/types.ts`                                  | `ExtraProperty` — `simulationBehavior`, `mutability` 추가         |
| **Frontend** | `entities/asset/index.ts`                                        | re-export 추가                                                    |
| **Frontend** | `shared/lib/canvasMetadata.ts`                                   | `EXTRA_PROPERTIES_KEY` 상수 추가                                  |
| **Frontend** | `shared/lib/useAssetMetadataForm.ts`                             | 신규 — 공통 훅                                                    |
| **Frontend** | `shared/ui/ExtraPropertiesSection.tsx`                           | 신규 — 공통 UI                                                    |
| **Frontend** | `features/edit-asset-on-panel/ui/EditAssetOnPanel.tsx`           | 훅 기반 교체                                                      |
| **Frontend** | `pages/canvas/ui/AddAssetModal.tsx`                              | 훅 기반 교체                                                      |
| **Backend**  | `Application/Simulation/Handlers/RunSimulationCommandHandler.cs` | `ResolveEffectiveProperties` 추가, `ComputeState` 시그니처 변경   |
| **Pipeline** | `pipelines/asset_pipeline.py`                                    | `build_effective_schema` 함수 추가                                |
| **Pipeline** | `workers/asset_worker.py`                                        | `process_health_updated`, `process_simulation_state_updated` 수정 |

백엔드 MongoDB / OpenAPI 스키마 / Kafka 이벤트 구조 변경 없음.

---

## 핵심 설계: effective property set

```
인스턴스의 전체 시뮬레이션 속성 집합
= ObjectType.resolvedProperties (스키마 정의)
  ∪ Asset.metadata.extraProperties → PropertyDefinition으로 변환
```

이 effective set을 기반으로 백엔드 시뮬레이터와 파이프라인 연산이 동작한다.
`extraProperties[].value`는 `PropertyDefinition.BaseValue` 역할을 한다 — 상태가 없을 때 초기값으로 사용된다.

---

## Phase 1: Frontend — 타입 계약 정의

> 모든 경로는 `servers/frontend/src/` 기준

### Step 1-1: `entities/asset/model/types.ts`

파일 최상단에 import를 추가하고, 기존 내용 끝에 `ExtraProperty` 인터페이스를 추가한다.

```ts
import type {
  DataType,
  SimulationBehavior,
  Mutability,
} from '@/entities/object-type-schema'

/** metadata.extraProperties 배열의 단일 항목.
 *  스키마 정의(PropertyDefinition)와 동일한 시뮬레이션 규칙을 따르는 인스턴스 확장 속성.
 *  - simulationBehavior / mutability 는 백엔드 시뮬레이터에서 실제 연산에 사용된다.
 *  - value 는 PropertyDefinition.BaseValue 역할: 초기 상태가 없을 때 시드값.
 *  - required / derivedRule 은 인스턴스 수준에서 불필요하므로 제외.
 */
export interface ExtraProperty {
  key: string
  dataType: DataType
  unit?: string
  value: unknown
  simulationBehavior: SimulationBehavior
  mutability: Mutability
  constraints?: Record<string, unknown>
}
```

### Step 1-2: `entities/asset/index.ts`

```ts
// 변경 전
export type {
  AssetDto,
  CreateAssetRequest,
  UpdateAssetRequest,
} from './model/types'

// 변경 후
export type {
  AssetDto,
  CreateAssetRequest,
  UpdateAssetRequest,
  ExtraProperty,
} from './model/types'
```

### Step 1-3: `shared/lib/canvasMetadata.ts`

import 블록 바로 아래에 한 줄 추가한다. 기존 함수는 수정하지 않는다.

```ts
export const EXTRA_PROPERTIES_KEY = 'extraProperties' as const
```

---

## Phase 2: Frontend — 공통 훅 & 컴포넌트

### Step 2-1: `shared/lib/useAssetMetadataForm.ts` (신규 파일)

```ts
import { useMemo, useState } from 'react'
import type { ExtraProperty } from '@/entities/asset'
import type {
  DataType,
  ObjectTypeSchemaDto,
} from '@/entities/object-type-schema'
import {
  buildMetadataFromTypeSelection,
  mergeAssetMetadataWithSchema,
  EXTRA_PROPERTIES_KEY,
} from './canvasMetadata'

export const emptyExtraProperty = (): ExtraProperty => ({
  key: '',
  dataType: 'String' as DataType,
  value: '',
  simulationBehavior: 'Settable',
  mutability: 'Mutable',
})

export function useAssetMetadataForm(
  initialType: string,
  initialMeta: Record<string, unknown>,
  objectTypeSchemas: ObjectTypeSchemaDto[],
) {
  const [type, setType] = useState(initialType)

  const [metadata, setMetadata] = useState<Record<string, unknown>>(() => {
    const merged = mergeAssetMetadataWithSchema(
      initialType,
      objectTypeSchemas,
      initialMeta,
    )
    const { [EXTRA_PROPERTIES_KEY]: _ignored, ...rest } = merged
    return rest
  })

  const [extraProperties, setExtraProperties] = useState<ExtraProperty[]>(
    () => {
      const raw = initialMeta[EXTRA_PROPERTIES_KEY]
      return Array.isArray(raw) ? (raw as ExtraProperty[]) : []
    },
  )

  const schema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )

  const extraKeys = useMemo(
    () =>
      Object.keys(metadata).filter(
        (k) => !schemaKeySet.has(k) && k !== EXTRA_PROPERTIES_KEY,
      ),
    [metadata, schemaKeySet],
  )

  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => {
      const built = buildMetadataFromTypeSelection(
        newType,
        objectTypeSchemas,
        prev,
      )
      const { [EXTRA_PROPERTIES_KEY]: _ignored, ...rest } = built
      return rest
    })
  }

  const setMetaValue = (key: string, raw: string) => {
    setMetadata((m) => ({ ...m, [key]: raw }))
  }

  const removeExtraKey = (key: string) => {
    setMetadata((m) => {
      const next = { ...m }
      delete next[key]
      return next
    })
  }

  const addExtraRow = () => {
    const k = `extra_${Date.now()}`
    setMetadata((m) => ({ ...m, [k]: '' }))
  }

  const addExtraProperty = () =>
    setExtraProperties((prev) => [...prev, emptyExtraProperty()])

  const updateExtraProperty = (index: number, patch: Partial<ExtraProperty>) =>
    setExtraProperties((prev) =>
      prev.map((p, i) => (i === index ? { ...p, ...patch } : p)),
    )

  const removeExtraProperty = (index: number) =>
    setExtraProperties((prev) => prev.filter((_, i) => i !== index))

  const buildFinalMetadata = (): Record<string, unknown> => ({
    ...metadata,
    ...(extraProperties.length > 0
      ? { [EXTRA_PROPERTIES_KEY]: extraProperties }
      : {}),
  })

  return {
    type,
    metadata,
    extraProperties,
    schemaProps,
    extraKeys,
    handleTypeChange,
    setMetaValue,
    removeExtraKey,
    addExtraRow,
    addExtraProperty,
    updateExtraProperty,
    removeExtraProperty,
    buildFinalMetadata,
  }
}
```

### Step 2-2: `shared/ui/ExtraPropertiesSection.tsx` (신규 파일)

`simulationBehavior`와 `mutability` 셀렉터를 포함한다.
백엔드 연산에 실제 사용되는 필드임을 UI 레이블로 명시한다.

```tsx
import type {
  DataType,
  SimulationBehavior,
  Mutability,
} from '@/entities/object-type-schema'
import type { ExtraProperty } from '@/entities/asset'
import { UnitSelect } from './UnitSelect'

export function ExtraPropertiesSection({
  extraProperties,
  onAdd,
  onUpdate,
  onRemove,
}: {
  extraProperties: ExtraProperty[]
  onAdd: () => void
  onUpdate: (index: number, patch: Partial<ExtraProperty>) => void
  onRemove: (index: number) => void
}) {
  return (
    <div className="assets-canvas-page__meta-section">
      <span>확장 속성 (extraProperties)</span>
      {extraProperties.map((p, i) => (
        <div key={i} className="assets-canvas-page__meta-row">
          <input
            placeholder="key"
            value={p.key}
            onChange={(e) => onUpdate(i, { key: e.target.value })}
            aria-label={`extra-prop-key-${i}`}
          />
          <select
            value={p.dataType}
            onChange={(e) =>
              onUpdate(i, { dataType: e.target.value as DataType })
            }
            aria-label={`extra-prop-datatype-${i}`}
          >
            {(
              [
                'Number',
                'String',
                'Boolean',
                'DateTime',
                'Array',
                'Object',
              ] as const
            ).map((dt) => (
              <option key={dt} value={dt}>
                {dt}
              </option>
            ))}
          </select>
          <select
            value={p.simulationBehavior}
            onChange={(e) =>
              onUpdate(i, {
                simulationBehavior: e.target.value as SimulationBehavior,
              })
            }
            aria-label={`extra-prop-behavior-${i}`}
            title="시뮬레이션 동작 (엔진에 반영됨)"
          >
            {(
              [
                'Constant',
                'Settable',
                'Rate',
                'Accumulator',
                'Derived',
              ] as const
            ).map((sb) => (
              <option key={sb} value={sb}>
                {sb}
              </option>
            ))}
          </select>
          <select
            value={p.mutability}
            onChange={(e) =>
              onUpdate(i, { mutability: e.target.value as Mutability })
            }
            aria-label={`extra-prop-mutability-${i}`}
          >
            <option value="Mutable">Mutable</option>
            <option value="Immutable">Immutable</option>
          </select>
          {p.dataType === 'Number' && (
            <UnitSelect
              compact
              value={p.unit}
              onChange={(unit) => onUpdate(i, { unit: unit || undefined })}
            />
          )}
          <input
            placeholder="초기값"
            value={String(p.value ?? '')}
            onChange={(e) => onUpdate(i, { value: e.target.value })}
            aria-label={`extra-prop-value-${i}`}
            title="초기값 (BaseValue — 상태가 없을 때 시드로 사용)"
          />
          <button type="button" onClick={() => onRemove(i)}>
            삭제
          </button>
        </div>
      ))}
      <button type="button" onClick={onAdd}>
        + 속성 추가
      </button>
    </div>
  )
}
```

### Step 2-3: `features/edit-asset-on-panel/ui/EditAssetOnPanel.tsx` (교체)

```tsx
import { useState, type FormEvent } from 'react'
import type { AssetDto } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { useAssetMetadataForm } from '@/shared/lib/useAssetMetadataForm'
import { ExtraPropertiesSection } from '@/shared/ui/ExtraPropertiesSection'

export function EditAssetOnPanel({
  asset,
  objectTypeSchemas,
  onClose,
  onSave,
  onDeleted,
}: {
  asset: AssetDto
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onSave: (type: string, metadata: Record<string, unknown>) => Promise<void>
  onDeleted: () => Promise<void>
}) {
  const form = useAssetMetadataForm(
    asset.type,
    (asset.metadata ?? {}) as Record<string, unknown>,
    objectTypeSchemas,
  )
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [showSystemInfo, setShowSystemInfo] = useState(false)

  const copyId = () => void navigator.clipboard.writeText(asset.id)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      await onSave(form.type.trim(), form.buildFinalMetadata())
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!window.confirm('이 에셋을 삭제할까요? 이 작업은 되돌릴 수 없습니다.'))
      return
    setDeleteError(null)
    setDeleting(true)
    try {
      await onDeleted()
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : '삭제 실패')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
      <div className="assets-canvas-page__side-panel-header">
        <h3>에셋 편집</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>
      <div className="assets-canvas-page__system-actions">
        <button
          type="button"
          onClick={copyId}
          className="assets-canvas-page__copy-id-btn"
        >
          ID 복사
        </button>
        <button
          type="button"
          onClick={() => setShowSystemInfo((v) => !v)}
          className="assets-canvas-page__toggle-system"
          aria-expanded={showSystemInfo}
        >
          {showSystemInfo ? '시스템 정보 접기' : '시스템 정보'}
        </button>
      </div>
      {showSystemInfo && (
        <div
          className="assets-canvas-page__system-fields"
          aria-label="시스템 정보"
        >
          <dl>
            <dt>id</dt>
            <dd>{asset.id}</dd>
            <dt>createdAt</dt>
            <dd>
              {asset.createdAt
                ? new Date(asset.createdAt).toLocaleString()
                : '-'}
            </dd>
            <dt>updatedAt</dt>
            <dd>
              {asset.updatedAt
                ? new Date(asset.updatedAt).toLocaleString()
                : '-'}
            </dd>
          </dl>
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <label>
          ObjectType
          <select
            value={form.type}
            onChange={(e) => form.handleTypeChange(e.target.value)}
            required
            aria-label="ObjectType"
          >
            <option value="">— 선택 —</option>
            {objectTypeSchemas.map((s) => (
              <option key={s.objectType} value={s.objectType}>
                {s.displayName} ({s.objectType})
              </option>
            ))}
            {form.type &&
            !objectTypeSchemas.some((s) => s.objectType === form.type) ? (
              <option value={form.type}>{form.type} (스키마 없음)</option>
            ) : null}
          </select>
        </label>

        {form.schemaProps.length > 0 && (
          <div className="assets-canvas-page__meta-section">
            <span>스키마 속성</span>
            {form.schemaProps.map((p) => (
              <div key={p.key} className="assets-canvas-page__meta-row">
                <input value={p.key} readOnly aria-label={`${p.key}-key`} />
                <input
                  value={String(form.metadata[p.key] ?? '')}
                  readOnly={p.mutability === 'Immutable'}
                  onChange={
                    p.mutability === 'Mutable'
                      ? (e) => form.setMetaValue(p.key, e.target.value)
                      : undefined
                  }
                  aria-label={`${p.key}-value`}
                />
                <span className="assets-canvas-page__prop-badge">
                  {p.dataType} / {p.simulationBehavior} / {p.mutability}
                </span>
              </div>
            ))}
          </div>
        )}

        <ExtraPropertiesSection
          extraProperties={form.extraProperties}
          onAdd={form.addExtraProperty}
          onUpdate={form.updateExtraProperty}
          onRemove={form.removeExtraProperty}
        />

        <div className="assets-canvas-page__meta-section">
          <span>추가 메타데이터 (스키마 외 키)</span>
          {form.extraKeys.map((key) => (
            <div key={key} className="assets-canvas-page__meta-row">
              <input
                placeholder="key"
                value={key}
                readOnly
                aria-label="extra-key"
              />
              <input
                placeholder="value"
                value={String(form.metadata[key] ?? '')}
                onChange={(e) => form.setMetaValue(key, e.target.value)}
              />
              <button type="button" onClick={() => form.removeExtraKey(key)}>
                삭제
              </button>
            </div>
          ))}
          <button type="button" onClick={form.addExtraRow}>
            항목 추가
          </button>
        </div>

        {saveError && <p className="assets-canvas-page__error">{saveError}</p>}
        <button type="submit" disabled={saving}>
          {saving ? '저장 중…' : '저장'}
        </button>
      </form>
      <div className="assets-canvas-page__side-panel-danger">
        {deleteError && (
          <p className="assets-canvas-page__error">{deleteError}</p>
        )}
        <button
          type="button"
          className="assets-canvas-page__delete-asset-btn"
          disabled={deleting}
          onClick={() => void handleDelete()}
        >
          {deleting ? '삭제 중…' : '에셋 삭제'}
        </button>
      </div>
    </>
  )
}
```

### Step 2-4: `pages/canvas/ui/AddAssetModal.tsx` (교체)

```tsx
import { useState, type FormEvent } from 'react'
import { createAsset } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { useAssetMetadataForm } from '@/shared/lib/useAssetMetadataForm'
import { ExtraPropertiesSection } from '@/shared/ui/ExtraPropertiesSection'

export function AddAssetModal({
  objectTypeSchemas,
  onClose,
  onCreated,
}: {
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onCreated: () => void
}) {
  const form = useAssetMetadataForm('', {}, objectTypeSchemas)
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const t = form.type.trim()
    if (!t) {
      setCreateError('ObjectType을 선택하세요')
      return
    }
    setSubmitting(true)
    try {
      await createAsset({
        type: t,
        connections: [],
        metadata: form.buildFinalMetadata(),
      })
      onCreated()
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : '생성 실패')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="assets-canvas-page__modal-overlay" onClick={onClose}>
      <div
        className="assets-canvas-page__modal"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="assets-canvas-page__side-panel-header">
          <h3>에셋 추가</h3>
          <button type="button" onClick={onClose} aria-label="닫기">
            ×
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          <label>
            ObjectType <span className="required">*</span>
            <select
              value={form.type}
              onChange={(e) => form.handleTypeChange(e.target.value)}
              required
            >
              <option value="">— 선택 —</option>
              {objectTypeSchemas.map((s) => (
                <option key={s.objectType} value={s.objectType}>
                  {s.displayName} ({s.objectType})
                </option>
              ))}
            </select>
          </label>

          {form.schemaProps.length > 0 && (
            <div className="assets-canvas-page__meta-section">
              <span>스키마 속성</span>
              {form.schemaProps.map((p) => (
                <div key={p.key} className="assets-canvas-page__meta-row">
                  <input
                    value={p.key}
                    readOnly
                    aria-label={`schema-${p.key}-key`}
                  />
                  <input
                    value={String(form.metadata[p.key] ?? '')}
                    readOnly={p.mutability === 'Immutable'}
                    onChange={
                      p.mutability === 'Mutable'
                        ? (e) => form.setMetaValue(p.key, e.target.value)
                        : undefined
                    }
                    aria-label={`schema-${p.key}-value`}
                  />
                </div>
              ))}
            </div>
          )}

          <ExtraPropertiesSection
            extraProperties={form.extraProperties}
            onAdd={form.addExtraProperty}
            onUpdate={form.updateExtraProperty}
            onRemove={form.removeExtraProperty}
          />

          <div className="assets-canvas-page__meta-section">
            <span>추가 메타데이터</span>
            {form.extraKeys.map((key) => (
              <div key={key} className="assets-canvas-page__meta-row">
                <input placeholder="key" value={key} readOnly />
                <input
                  placeholder="value"
                  value={String(form.metadata[key] ?? '')}
                  onChange={(e) => form.setMetaValue(key, e.target.value)}
                />
                <button type="button" onClick={() => form.removeExtraKey(key)}>
                  삭제
                </button>
              </div>
            ))}
            <button type="button" onClick={form.addExtraRow}>
              항목 추가
            </button>
          </div>

          {createError && (
            <p className="assets-canvas-page__error">{createError}</p>
          )}
          <button type="submit" disabled={submitting}>
            {submitting ? '생성 중…' : '생성'}
          </button>
        </form>
      </div>
    </div>
  )
}
```

### Step 2-5: 프론트엔드 타입 검사

```bash
cd servers/frontend && pnpm tsc --noEmit
```

에러 0건이어야 한다.

---

## Phase 3: Backend — ResolveEffectiveProperties

**파일**: `servers/backend/DotnetEngine/Application/Simulation/Handlers/RunSimulationCommandHandler.cs`

### 변경 1: `ComputeState` 시그니처에 `AssetDto?` 추가

```csharp
// 변경 전 시그니처
private StateDto ComputeState(
    string assetId,
    StateDto? current,
    StatePatchDto patch,
    ObjectTypeSchemaDto? objectTypeSchema)

// 변경 후 시그니처
private StateDto ComputeState(
    string assetId,
    StateDto? current,
    StatePatchDto patch,
    ObjectTypeSchemaDto? objectTypeSchema,
    AssetDto? asset = null)
```

### 변경 2: `ComputeState` 내부 — `schemaProperties` 조립 방식 변경

기존:

```csharp
var schemaProperties = objectTypeSchema.ResolvedProperties ?? objectTypeSchema.OwnProperties;
```

변경 후:

```csharp
var schemaProperties = ResolveEffectiveProperties(objectTypeSchema, asset);
```

### 변경 3: `RunOnePropagationAsync` — `ComputeState` 호출 시 `asset` 전달

`asset`은 이미 같은 스코프에 선언되어 있으므로 인자를 추가만 하면 된다.

기존:

```csharp
var mergedState = ComputeState(assetId, currentState, patch, objectTypeSchema);
```

변경 후 (두 군데 — BFS 본문과 사이클 처리 블록 모두):

```csharp
var mergedState = ComputeState(assetId, currentState, patch, objectTypeSchema, asset);
```

> 사이클 처리 블록(line 210~237)에서도 `asset`이 이미 선언되어 있으므로 동일하게 전달한다.

### 변경 4: `ResolveEffectiveProperties` 헬퍼 추가

클래스 내부 (private static 메서드로) 추가한다:

```csharp
/// <summary>
/// ObjectType 스키마의 속성과 asset.metadata.extraProperties를 합쳐
/// 시뮬레이션에 사용할 effective 속성 목록을 반환한다.
/// </summary>
private static IReadOnlyList<PropertyDefinition> ResolveEffectiveProperties(
    ObjectTypeSchemaDto? schema,
    AssetDto? asset)
{
    var schemaProps = (IEnumerable<PropertyDefinition>)
        (schema?.ResolvedProperties ?? schema?.OwnProperties ?? []);

    if (asset?.Metadata == null ||
        !asset.Metadata.TryGetValue("extraProperties", out var raw) ||
        raw is not System.Collections.IEnumerable items)
        return schemaProps.ToList();

    var extraDefs = ParseExtraProperties(items);
    return [.. schemaProps, .. extraDefs];
}

/// <summary>
/// metadata["extraProperties"] BSON/JSON 배열을 PropertyDefinition 목록으로 변환한다.
/// 파싱 실패한 항목은 무시한다.
/// </summary>
private static List<PropertyDefinition> ParseExtraProperties(
    System.Collections.IEnumerable items)
{
    var result = new List<PropertyDefinition>();

    foreach (var item in items)
    {
        if (item is not IDictionary<string, object> dict)
            continue;

        if (!dict.TryGetValue("key", out var keyObj) || keyObj?.ToString() is not { } key || key == "")
            continue;

        var dataTypeStr = dict.TryGetValue("dataType", out var dt) ? dt?.ToString() : null;
        var simBehaviorStr = dict.TryGetValue("simulationBehavior", out var sb) ? sb?.ToString() : null;
        var mutabilityStr = dict.TryGetValue("mutability", out var m) ? m?.ToString() : null;

        if (!Enum.TryParse<DataType>(dataTypeStr, ignoreCase: true, out var dataType))
            dataType = DataType.String;
        if (!Enum.TryParse<SimulationBehavior>(simBehaviorStr, ignoreCase: true, out var simBehavior))
            simBehavior = SimulationBehavior.Settable;
        if (!Enum.TryParse<Mutability>(mutabilityStr, ignoreCase: true, out var mutability))
            mutability = Mutability.Mutable;

        dict.TryGetValue("unit", out var unitObj);
        dict.TryGetValue("value", out var baseValue);

        IReadOnlyDictionary<string, object?> constraints = new Dictionary<string, object?>();
        if (dict.TryGetValue("constraints", out var constraintsObj) &&
            constraintsObj is IDictionary<string, object> cDict)
        {
            constraints = cDict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }

        result.Add(new PropertyDefinition
        {
            Key = key,
            DataType = dataType,
            Unit = unitObj?.ToString(),
            SimulationBehavior = simBehavior,
            Mutability = mutability,
            BaseValue = baseValue,
            Constraints = constraints,
            Required = false,
        });
    }

    return result;
}
```

> **주의**: MongoDB C# 드라이버가 BSON 배열/오브젝트를 어떤 타입으로 역직렬화하는지는 드라이버 설정에 따라 다를 수 있다.
> `IDictionary<string, object>` 캐스팅이 실패하면 `BsonDocument`를 먼저 확인하거나,
> 프로젝트에서 이미 사용하는 BSON 변환 헬퍼(있다면)를 재사용한다.

---

## Phase 4: Pipeline — build_effective_schema

**파일**: `servers/pipeline/src/pipelines/asset_pipeline.py`

### 변경 1: `build_effective_schema` 함수 추가

기존 함수들 아래에 추가한다.

```python
def build_effective_schema(
    schema: dict[str, Any] | None,
    asset_metadata: dict[str, Any] | None,
) -> dict[str, Any]:
    """
    ObjectType 스키마의 ownProperties와 asset.metadata.extraProperties를 합쳐
    파이프라인 연산(calculate_derived_properties, _evaluate_alert_thresholds)에
    사용할 effective 스키마를 반환한다.

    extraProperties 항목은 PropertyDefinition과 동일한 형태로 변환되어 ownProperties 뒤에 추가된다.
    """
    base_props: list[dict[str, Any]] = list((schema or {}).get("ownProperties", []))
    extra_props = (asset_metadata or {}).get("extraProperties", [])

    if not isinstance(extra_props, list):
        return dict(schema) if schema else {}

    converted: list[dict[str, Any]] = []
    for ep in extra_props:
        if not isinstance(ep, dict):
            continue
        key = ep.get("key")
        if not key:
            continue
        converted.append({
            "key": key,
            "dataType": ep.get("dataType", "String"),
            "unit": ep.get("unit"),
            "simulationBehavior": ep.get("simulationBehavior", "Settable"),
            "mutability": ep.get("mutability", "Mutable"),
            "baseValue": ep.get("value"),   # value → baseValue 매핑
            "constraints": ep.get("constraints") or {},
            "alertThresholds": [],           # 현재 extraProperties는 alertThresholds 미지원
        })

    effective = dict(schema) if schema else {}
    effective["ownProperties"] = base_props + converted
    return effective
```

**파일**: `servers/pipeline/src/workers/asset_worker.py`

### 변경 2: import에 `build_effective_schema` 추가

```python
from pipelines.asset_pipeline import (
    asset_state_to_dto,
    build_alert_event,
    build_effective_schema,       # 추가
    calculate_derived_properties,
    calculate_state,
)
```

### 변경 3: `process_health_updated` — effective schema 사용

```python
def process_health_updated(self, event: AssetHealthUpdatedEventDto) -> None:
    logger.info(f"Processing asset.health.updated: {event.asset_id}")

    asset_doc = self.repository.get_asset(event.asset_id)
    asset_type = (asset_doc or {}).get("type", "unknown") if asset_doc else "unknown"
    asset_metadata = (asset_doc or {}).get("metadata", {}) if asset_doc else {}  # 추가

    schema = self.object_type_repository.get_by_object_type(str(asset_type))
    effective_schema = build_effective_schema(schema, asset_metadata)             # 추가

    state = calculate_state(event, asset_type=str(asset_type), schema=effective_schema)  # schema → effective_schema
    state_dto = asset_state_to_dto(state)

    self.repository.save_state(state_dto)
    self.repository.save_event(
        asset_id=event.asset_id,
        event_type=event.event_type,
        timestamp=event.timestamp,
        payload=event.payload,
    )

    if state.status in (AssetConstants.Status.WARNING, AssetConstants.Status.ERROR):
        run_id = event.payload.get("runId")
        alert_payload = build_alert_event(
            asset_id=state.asset_id,
            timestamp=event.timestamp,
            status=state.status,
            properties=state.properties,
            run_id=run_id,
        )
        self.producer.send(self.settings.kafka_topic_alert_events, value=alert_payload)

    self._generate_recommendations(state.asset_id, event.payload.get("type", "asset"))
```

### 변경 4: `process_simulation_state_updated` — effective schema 사용

```python
def process_simulation_state_updated(self, event: SimulationStateUpdatedEventDto) -> None:
    logger.info(f"Processing simulation.state.updated: {event.asset_id}")

    asset_doc = self.repository.get_asset(event.asset_id)
    asset_type = (asset_doc or {}).get("type", "unknown") if asset_doc else "unknown"
    asset_metadata = (asset_doc or {}).get("metadata", {}) if asset_doc else {}  # 추가

    schema = self.object_type_repository.get_by_object_type(str(asset_type))
    effective_schema = build_effective_schema(schema, asset_metadata)             # 추가

    payload = dict(event.payload)
    props = payload.get("properties")
    if not isinstance(props, dict):
        props = {}
    merged_props = dict(props)
    if effective_schema:                                                           # schema → effective_schema
        delta_seconds = float(payload.get("deltaSeconds", 1.0))
        merged_props = {**props, **calculate_derived_properties(dict(props), effective_schema, delta_seconds)}
    payload["properties"] = merged_props

    event_merged = event.model_copy(update={"payload": payload})
    state = calculate_state(event_merged, asset_type=str(asset_type), schema=effective_schema)  # schema → effective_schema

    state_dto = asset_state_to_dto(state)
    self.repository.save_state(state_dto)
    self.repository.save_event(
        asset_id=event.asset_id,
        event_type=event.event_type,
        timestamp=event.timestamp,
        payload=event.payload,
    )

    if state.status in (AssetConstants.Status.WARNING, AssetConstants.Status.ERROR):
        run_id = event.payload.get("runId")
        alert_payload = build_alert_event(
            asset_id=state.asset_id,
            timestamp=event.timestamp,
            status=state.status,
            properties=state.properties,
            run_id=run_id,
        )
        self.producer.send(self.settings.kafka_topic_alert_events, value=alert_payload)

    self._generate_recommendations(state.asset_id, event.payload.get("type", "asset"))
```

---

## Phase 5: 검증

### 5-1: 프론트엔드 타입 검사

```bash
cd servers/frontend && pnpm tsc --noEmit
```

### 5-2: 백엔드 빌드

```bash
cd servers/backend && dotnet build DotnetEngine/DotnetEngine.csproj
```

빌드 에러 0건.

### 5-3: 파이프라인 타입 검사 (있는 경우)

```bash
cd servers/pipeline && python -m mypy src/ --ignore-missing-imports
```

### 5-4: 동작 검증 시나리오

**시나리오 A — extraProperty가 State에 포함되는지**

1. 에셋 생성 시 extraProperties에 `{ key: "pressure", dataType: "Number", value: 5.0, simulationBehavior: "Settable", mutability: "Mutable" }` 추가
2. 시뮬레이션 실행
3. `GET /api/assets/:id/state` 응답의 `properties`에 `"pressure": 5.0` 포함 확인

**시나리오 B — Immutable extraProperty는 패치 무시**

1. extraProperty를 `mutability: "Immutable"` 로 설정 후 저장
2. 시뮬레이션 실행 중 해당 키로 StatePatch 전달
3. State의 해당 키 값이 변하지 않는지 확인

**시나리오 C — Constant extraProperty는 항상 초기값 유지**

1. extraProperty를 `simulationBehavior: "Constant"`, `value: 100` 으로 설정
2. 시뮬레이션 여러 틱 실행
3. 해당 키가 항상 `100`으로 유지되는지 확인

**시나리오 D — 파이프라인이 extraProperty를 연산에 포함**

1. Derived extraProperty 설정 (다른 키 참조)
2. `simulation.state.updated` 이벤트 발생
3. 파이프라인이 처리한 State MongoDB 도큐먼트에 derived 값 반영 확인

**시나리오 E — 스키마 속성과 extraProperty가 동시에 동작**

1. ObjectType에 `temperature` ownProperty 있는 에셋에 `pressure` extraProperty 추가
2. 시뮬레이션 실행
3. State.properties에 `temperature`와 `pressure` 모두 포함 확인

---

## 변경 흐름 요약

```
에셋 저장 (PUT /api/assets/:id)
  metadata.extraProperties = [{ key, dataType, value, simulationBehavior, mutability }]
          │
          ▼
백엔드 RunSimulationCommandHandler.ComputeState()
  ResolveEffectiveProperties(schema, asset)
  = schema.resolvedProperties ∪ extraProperties → PropertyDefinition[]
  → 동일한 IPropertySimulator 파이프라인 실행
  → StateDto.Properties에 extraProperty 키 포함
          │
          ▼
Kafka: simulation.state.updated { payload.properties: { ..., [extraProp.key]: value } }
          │
          ▼
파이프라인 AssetWorker.process_simulation_state_updated()
  build_effective_schema(schema, asset_metadata)
  → calculate_derived_properties(effective_schema)  ← extraProperty Derived 연산
  → _evaluate_alert_thresholds(effective_schema)    ← (alertThresholds 추후 지원 가능)
  → MongoDB states 컬렉션 저장
```
