import { useMemo, useState, type FormEvent } from 'react'
import type { AssetDto } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import {
  buildMetadataFromTypeSelection,
  mergeAssetMetadataWithSchema,
} from '@/shared/lib/canvasMetadata'

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
  const [metadata, setMetadata] = useState<Record<string, unknown>>(() =>
    mergeAssetMetadataWithSchema(
      asset.type,
      objectTypeSchemas,
      (asset.metadata ?? {}) as Record<string, unknown>,
    ),
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
  const extraKeys = useMemo(
    () => Object.keys(metadata).filter((k) => !schemaKeySet.has(k)),
    [metadata, schemaKeySet],
  )

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

  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => buildMetadataFromTypeSelection(newType, objectTypeSchemas, prev))
  }

  const copyId = () => {
    void navigator.clipboard.writeText(asset.id)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      await onSave(type.trim(), metadata)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!window.confirm('이 에셋을 삭제할까요? 이 작업은 되돌릴 수 없습니다.')) return
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
        <button type="button" onClick={copyId} className="assets-canvas-page__copy-id-btn">
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
        <div className="assets-canvas-page__system-fields" aria-label="시스템 정보">
          <dl>
            <dt>id</dt>
            <dd>{asset.id}</dd>
            <dt>createdAt</dt>
            <dd>{asset.createdAt ? new Date(asset.createdAt).toLocaleString() : '-'}</dd>
            <dt>updatedAt</dt>
            <dd>{asset.updatedAt ? new Date(asset.updatedAt).toLocaleString() : '-'}</dd>
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
        {deleteError && <p className="assets-canvas-page__error">{deleteError}</p>}
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
