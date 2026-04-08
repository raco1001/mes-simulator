import { useState, type FormEvent } from 'react'
import type { AssetDto } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import {
  ASSET_NAME_KEY,
  getMetadataAssetName,
} from '@/shared/lib/canvasMetadata'
import { useAssetMetadataForm } from '@/shared/lib/useAssetMetadataForm'
import {
  ExtraPropertiesSection,
  FlatExtraMetadataSection,
} from '@/shared/ui/ExtraPropertiesSection'
import { SchemaPropertyRows } from '@/shared/ui/SchemaPropertyRows'

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
  const {
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
    buildMetadataForSave,
  } = useAssetMetadataForm({
    objectTypeSchemas,
    resetKey: asset.id,
    initialType: asset.type,
    initialMetadata: (asset.metadata ?? {}) as Record<string, unknown>,
    mergeOnInit: true,
  })

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [showSystemInfo, setShowSystemInfo] = useState(false)

  const copyId = () => {
    void navigator.clipboard.writeText(asset.id)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      await onSave(type.trim(), buildMetadataForSave())
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
        <label>
          표시 이름 (선택)
          <input
            type="text"
            value={getMetadataAssetName(metadata)}
            onChange={(e) => setMetaValue(ASSET_NAME_KEY, e.target.value)}
            placeholder="캔버스에 표시할 이름"
            autoComplete="off"
            aria-label="asset-display-name"
          />
        </label>

        <SchemaPropertyRows
          schemaProps={schemaProps}
          metadata={metadata}
          setMetaValue={setMetaValue}
        />

        <ExtraPropertiesSection
          resetKey={asset.id}
          extraProperties={extraProperties}
          onAdd={addExtraProperty}
          onUpdate={updateExtraProperty}
          onRemove={removeExtraProperty}
        />

        <FlatExtraMetadataSection
          extraKeys={extraKeys}
          metadata={metadata}
          onSetValue={setMetaValue}
          onRemoveKey={removeExtraKey}
          onAddRow={addExtraRow}
          addButtonLabel="항목 추가"
        />

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
