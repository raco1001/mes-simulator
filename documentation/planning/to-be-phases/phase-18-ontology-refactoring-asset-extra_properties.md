# 구현 계획: `metadata.extraProperties` 인스턴스 확장 속성 편집 기능

## 개요

Asset 편집 패널에서 `metadata.extraProperties` 필드를 ObjectType의 `ownProperties`와 동일한 방식(key, dataType, unit, value)으로 편집할 수 있도록 한다.

- **변경 파일: 2개** (프론트엔드만)
- **백엔드 / MongoDB / OpenAPI 변경 없음**

### 설계 확정 사항

| 항목         | 결정                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------- |
| JSON 형태    | 배열 (`ExtraProperty[]`) — `ownProperties`와 동일                                                             |
| 필드 구성    | `key`, `dataType`, `unit?`, `value` — 인스턴스 수준이므로 `simulationBehavior`, `mutability`, `required` 제외 |
| 저장 위치    | `metadata.extraProperties` (기존 평면 extra 키와 분리)                                                        |
| 타입 변경 시 | `extraProperties` 상태 유지 (별도 state로 관리하므로 자동 보존)                                               |
| 빈 배열 처리 | `extraProperties`가 비어있으면 metadata에 키 자체를 포함하지 않음                                             |

---

## Step 1: `canvasMetadata.ts` — 예약 키 상수 추가

**파일**: `servers/frontend/src/shared/lib/canvasMetadata.ts`

### 변경 내용

파일 상단에 상수를 export 추가한다.

**현재 코드 (1번째 줄):**

```ts
import type {
  ObjectTypeSchemaDto,
  PropertyDefinition,
  SimulationBehavior,
} from '@/entities/object-type-schema'
```

**변경 후:**

```ts
import type {
  ObjectTypeSchemaDto,
  PropertyDefinition,
  SimulationBehavior,
} from '@/entities/object-type-schema'

/** metadata 안의 예약 키 — extraProperties는 별도 state로 관리되므로 flat extra 목록에서 제외 */
export const EXTRA_PROPERTIES_KEY = 'extraProperties' as const
```

> `buildMetadataFromTypeSelection`과 `mergeAssetMetadataWithSchema` 함수 본문은 수정하지 않는다.
> `extraProperties`는 스키마 키가 아니므로 기존 preserved 로직에 의해 이미 자동 보존된다.

---

## Step 2: `EditAssetOnPanel.tsx` — 전체 재작성

**파일**: `servers/frontend/src/features/edit-asset-on-panel/ui/EditAssetOnPanel.tsx`

아래 전체 코드로 파일을 교체한다.

```tsx
import { useMemo, useState, type FormEvent } from 'react'
import type { AssetDto } from '@/entities/asset'
import type {
  DataType,
  ObjectTypeSchemaDto,
} from '@/entities/object-type-schema'
import {
  buildMetadataFromTypeSelection,
  mergeAssetMetadataWithSchema,
  EXTRA_PROPERTIES_KEY,
} from '@/shared/lib/canvasMetadata'
import { UnitSelect } from '@/shared/ui/UnitSelect'

/** 인스턴스 수준 확장 속성 — ObjectType의 PropertyDefinition 보다 단순한 형태 */
interface ExtraProperty {
  key: string
  dataType: DataType
  unit?: string
  value: unknown
}

const emptyExtraProperty = (): ExtraProperty => ({
  key: '',
  dataType: 'String',
  value: '',
})

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
  const [type, setType] = useState(asset.type)

  // metadata state에서는 extraProperties 키를 제거하여 별도 state로 분리 관리
  const [metadata, setMetadata] = useState<Record<string, unknown>>(() => {
    const merged = mergeAssetMetadataWithSchema(
      asset.type,
      objectTypeSchemas,
      (asset.metadata ?? {}) as Record<string, unknown>,
    )
    const { [EXTRA_PROPERTIES_KEY]: _ignored, ...rest } = merged
    return rest
  })

  // extraProperties는 metadata와 독립적인 state
  const [extraProperties, setExtraProperties] = useState<ExtraProperty[]>(
    () => {
      const raw = ((asset.metadata ?? {}) as Record<string, unknown>)[
        EXTRA_PROPERTIES_KEY
      ]
      return Array.isArray(raw) ? (raw as ExtraProperty[]) : []
    },
  )

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [showSystemInfo, setShowSystemInfo] = useState(false)

  const schema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )

  // EXTRA_PROPERTIES_KEY는 별도 state로 관리하므로 flat extra 목록에서 제외
  const extraKeys = useMemo(
    () =>
      Object.keys(metadata).filter(
        (k) => !schemaKeySet.has(k) && k !== EXTRA_PROPERTIES_KEY,
      ),
    [metadata, schemaKeySet],
  )

  // ── 스키마/flat extra 헬퍼 ──────────────────────────────────────────────
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

  // ── extraProperties 헬퍼 ────────────────────────────────────────────────
  const addExtraProperty = () => {
    setExtraProperties((prev) => [...prev, emptyExtraProperty()])
  }

  const updateExtraProperty = (
    index: number,
    patch: Partial<ExtraProperty>,
  ) => {
    setExtraProperties((prev) =>
      prev.map((p, i) => (i === index ? { ...p, ...patch } : p)),
    )
  }

  const removeExtraProperty = (index: number) => {
    setExtraProperties((prev) => prev.filter((_, i) => i !== index))
  }

  // ── 타입 변경 ────────────────────────────────────────────────────────────
  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => {
      const built = buildMetadataFromTypeSelection(
        newType,
        objectTypeSchemas,
        prev,
      )
      // 빌드된 결과에서도 extraProperties 키 제거 (별도 state 유지)
      const { [EXTRA_PROPERTIES_KEY]: _ignored, ...rest } = built
      return rest
    })
    // extraProperties 상태는 타입 변경과 무관하게 유지됨
  }

  const copyId = () => {
    void navigator.clipboard.writeText(asset.id)
  }

  // ── 저장 ────────────────────────────────────────────────────────────────
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      const finalMetadata: Record<string, unknown> = { ...metadata }
      if (extraProperties.length > 0) {
        finalMetadata[EXTRA_PROPERTIES_KEY] = extraProperties
      }
      await onSave(type.trim(), finalMetadata)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  // ── 삭제 ────────────────────────────────────────────────────────────────
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
            value={type}
            onChange={(e) => handleTypeChange(e.target.value)}
            required
            aria-label="ObjectType"
          >
            <option value="">— 선택 —</option>
            {objectTypeSchemas.map((s) => (
              <option key={s.objectType} value={s.objectType}>
                {s.displayName} ({s.objectType})
              </option>
            ))}
            {type && !objectTypeSchemas.some((s) => s.objectType === type) ? (
              <option value={type}>{type} (스키마 없음)</option>
            ) : null}
          </select>
        </label>

        {/* 스키마 속성 (ObjectType에서 정의된 속성) */}
        {schemaProps.length > 0 && (
          <div className="assets-canvas-page__meta-section">
            <span>스키마 속성</span>
            {schemaProps.map((p) => (
              <div key={p.key} className="assets-canvas-page__meta-row">
                <input value={p.key} readOnly aria-label={`${p.key}-key`} />
                <input
                  value={String(metadata[p.key] ?? '')}
                  readOnly={p.mutability === 'Immutable'}
                  onChange={
                    p.mutability === 'Mutable'
                      ? (e) => setMetaValue(p.key, e.target.value)
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

        {/* 인스턴스 확장 속성 (extraProperties) — 단위·타입 지정 가능 */}
        <div className="assets-canvas-page__meta-section">
          <span>확장 속성 (extraProperties)</span>
          {extraProperties.map((p, i) => (
            <div key={i} className="assets-canvas-page__meta-row">
              <input
                placeholder="key"
                value={p.key}
                onChange={(e) =>
                  updateExtraProperty(i, { key: e.target.value })
                }
                aria-label={`extra-prop-key-${i}`}
              />
              <select
                value={p.dataType}
                onChange={(e) =>
                  updateExtraProperty(i, {
                    dataType: e.target.value as DataType,
                  })
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
              {p.dataType === 'Number' && (
                <UnitSelect
                  value={p.unit}
                  onChange={(unit) =>
                    updateExtraProperty(i, { unit: unit || undefined })
                  }
                />
              )}
              <input
                placeholder="value"
                value={String(p.value ?? '')}
                onChange={(e) =>
                  updateExtraProperty(i, { value: e.target.value })
                }
                aria-label={`extra-prop-value-${i}`}
              />
              <button type="button" onClick={() => removeExtraProperty(i)}>
                삭제
              </button>
            </div>
          ))}
          <button type="button" onClick={addExtraProperty}>
            + 속성 추가
          </button>
        </div>

        {/* 추가 메타데이터 — 스키마/extraProperties 외의 평면 키 (레거시/시스템 키) */}
        <div className="assets-canvas-page__meta-section">
          <span>추가 메타데이터 (스키마 외 키)</span>
          {extraKeys.map((key) => (
            <div key={key} className="assets-canvas-page__meta-row">
              <input
                placeholder="key"
                value={key}
                readOnly
                aria-label="extra-key"
              />
              <input
                placeholder="value"
                value={String(metadata[key] ?? '')}
                onChange={(e) => setMetaValue(key, e.target.value)}
              />
              <button type="button" onClick={() => removeExtraKey(key)}>
                삭제
              </button>
            </div>
          ))}
          <button type="button" onClick={addExtraRow}>
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

---

## 검증 체크리스트

에이전트는 아래 항목을 순서대로 확인한다.

### 정적 검증

```bash
cd servers/frontend && pnpm tsc --noEmit
```

타입 에러 0건이어야 한다.

### 동작 검증 (수동)

1. **신규 extraProperty 추가**: 에셋 편집 패널에서 "확장 속성 + 속성 추가" 클릭 → key/dataType/value 입력 후 저장 → 저장된 asset의 `metadata.extraProperties`가 배열로 들어있는지 확인
2. **Number 타입 선택 시 UnitSelect 표시**: dataType을 `Number`로 변경하면 UnitSelect가 나타나는지 확인
3. **불러오기 복원**: 기존에 `extraProperties`가 저장된 asset을 편집 패널로 열면 값이 복원되는지 확인
4. **타입 변경 후 유지**: ObjectType을 변경해도 `extraProperties` 목록이 사라지지 않는지 확인
5. **flat extra 키 분리**: 기존 평면 extra 키(`extra_xxxx` 등)가 "추가 메타데이터" 섹션에만 표시되고 "확장 속성" 섹션과 섞이지 않는지 확인
6. **빈 배열 저장**: 모든 extraProperty를 삭제하고 저장하면 `metadata.extraProperties` 키가 생략되는지 확인

---

## 변경 파일 요약

| 파일                                                                        | 변경 유형  | 내용                                                           |
| --------------------------------------------------------------------------- | ---------- | -------------------------------------------------------------- |
| `servers/frontend/src/shared/lib/canvasMetadata.ts`                         | 추가 (2줄) | `EXTRA_PROPERTIES_KEY` 상수 export                             |
| `servers/frontend/src/features/edit-asset-on-panel/ui/EditAssetOnPanel.tsx` | 교체       | `ExtraProperty` 인터페이스 + 관련 state/helpers + UI 섹션 추가 |
