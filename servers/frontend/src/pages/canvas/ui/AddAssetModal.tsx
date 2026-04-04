import { useMemo, useState, type FormEvent } from 'react'
import { createAsset } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { buildMetadataFromTypeSelection } from '@/shared/lib/canvasMetadata'

export function AddAssetModal({
  objectTypeSchemas,
  onClose,
  onCreated,
}: {
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onCreated: () => void
}) {
  const [type, setType] = useState('')
  const [metadata, setMetadata] = useState<Record<string, unknown>>({})
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const selectedSchema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = selectedSchema?.resolvedProperties ?? selectedSchema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )
  const extraKeys = useMemo(
    () => Object.keys(metadata).filter((k) => !schemaKeySet.has(k)),
    [metadata, schemaKeySet],
  )

  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => buildMetadataFromTypeSelection(newType, objectTypeSchemas, prev))
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

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const t = type.trim()
    if (!t) {
      setCreateError('ObjectType을 선택하세요')
      return
    }
    setSubmitting(true)
    try {
      await createAsset({
        type: t,
        connections: [],
        metadata: Object.keys(metadata).length ? metadata : {},
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
      <div className="assets-canvas-page__modal" onClick={(e) => e.stopPropagation()}>
        <div className="assets-canvas-page__side-panel-header">
          <h3>에셋 추가</h3>
          <button type="button" onClick={onClose} aria-label="닫기">
            ×
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          <label>
            ObjectType <span className="required">*</span>
            <select value={type} onChange={(e) => handleTypeChange(e.target.value)} required>
              <option value="">— 선택 —</option>
              {objectTypeSchemas.map((schema) => (
                <option key={schema.objectType} value={schema.objectType}>
                  {schema.displayName} ({schema.objectType})
                </option>
              ))}
            </select>
          </label>
          {schemaProps.length > 0 && (
            <div className="assets-canvas-page__meta-section">
              <span>스키마 속성</span>
              {schemaProps.map((p) => (
                <div key={p.key} className="assets-canvas-page__meta-row">
                  <input value={p.key} readOnly aria-label={`schema-${p.key}-key`} />
                  <input
                    value={String(metadata[p.key] ?? '')}
                    readOnly={p.mutability === 'Immutable'}
                    onChange={
                      p.mutability === 'Mutable'
                        ? (e) => setMetaValue(p.key, e.target.value)
                        : undefined
                    }
                    aria-label={`schema-${p.key}-value`}
                  />
                </div>
              ))}
            </div>
          )}
          <div className="assets-canvas-page__meta-section">
            <span>추가 메타데이터</span>
            {extraKeys.map((key) => (
              <div key={key} className="assets-canvas-page__meta-row">
                <input placeholder="key" value={key} readOnly />
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
          {createError && <p className="assets-canvas-page__error">{createError}</p>}
          <button type="submit" disabled={submitting}>
            {submitting ? '생성 중…' : '생성'}
          </button>
        </form>
      </div>
    </div>
  )
}
