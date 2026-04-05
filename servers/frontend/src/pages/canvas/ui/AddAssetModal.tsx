import { useState, type FormEvent } from 'react'
import { createAsset } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { useAssetMetadataForm } from '@/shared/lib/useAssetMetadataForm'
import {
  ExtraPropertiesSection,
  FlatExtraMetadataSection,
} from '@/shared/ui/ExtraPropertiesSection'

const CREATE_INITIAL_METADATA: Record<string, unknown> = {}

export function AddAssetModal({
  objectTypeSchemas,
  onClose,
  onCreated,
}: {
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onCreated: () => void
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
    resetKey: 'create',
    initialType: '',
    initialMetadata: CREATE_INITIAL_METADATA,
    mergeOnInit: false,
  })

  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

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
      const meta = buildMetadataForSave()
      await createAsset({
        type: t,
        connections: [],
        metadata: Object.keys(meta).length ? meta : {},
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

          <ExtraPropertiesSection
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

          {createError && <p className="assets-canvas-page__error">{createError}</p>}
          <button type="submit" disabled={submitting}>
            {submitting ? '생성 중…' : '생성'}
          </button>
        </form>
      </div>
    </div>
  )
}
