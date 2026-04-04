import { useMemo, useState, type FormEvent } from 'react'
import {
  updateRelationship,
  deleteRelationship,
  type RelationshipDto,
  type PropertyMapping,
} from '@/entities/relationship'
import type { LinkTypeSchemaDto } from '@/entities/link-type-schema'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { isEligibleProperty } from '@/shared/lib/canvasMetadata'
import './CreateRelationshipOnPanel.css'

interface MappingRow {
  fromProperty: string
  toProperty: string
  transformRule: string
}

function toRows(mappings: PropertyMapping[] | undefined): MappingRow[] {
  if (!mappings || mappings.length === 0) return []
  return mappings.map((m) => ({
    fromProperty: m.fromProperty,
    toProperty: m.toProperty,
    transformRule: m.transformRule ?? 'value',
  }))
}

export function EditRelationshipOnPanel({
  relationship,
  linkTypeSchemas,
  objectTypeSchemas,
  fromAssetType,
  toAssetType,
  onClose,
  onSaved,
  onDeleted,
}: {
  relationship: RelationshipDto
  linkTypeSchemas: LinkTypeSchemaDto[]
  objectTypeSchemas: ObjectTypeSchemaDto[]
  fromAssetType?: string | null
  toAssetType?: string | null
  onClose: () => void
  onSaved: (updated: RelationshipDto) => void
  onDeleted: (id: string) => void
}) {
  const [relationshipType, setRelationshipType] = useState(relationship.relationshipType)
  const [mappings, setMappings] = useState<MappingRow[]>(() => toRows(relationship.mappings))
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  const typeOptions = linkTypeSchemas.length > 0
    ? linkTypeSchemas.map((s) => s.linkType)
    : [relationship.relationshipType]

  // ── Schema-derived property lists ──
  const sourceSchema = fromAssetType
    ? objectTypeSchemas.find((s) => s.objectType === fromAssetType) ?? null
    : null
  const targetSchema = toAssetType
    ? objectTypeSchemas.find((s) => s.objectType === toAssetType) ?? null
    : null

  const sourceProps = useMemo(
    () => (sourceSchema ? (sourceSchema.resolvedProperties ?? sourceSchema.ownProperties).filter(isEligibleProperty) : []),
    [sourceSchema],
  )
  const targetProps = useMemo(
    () => (targetSchema ? (targetSchema.resolvedProperties ?? targetSchema.ownProperties).filter(isEligibleProperty) : []),
    [targetSchema],
  )

  const getUnit = (props: typeof sourceProps, key: string) =>
    props.find((p) => p.key === key)?.unit ?? null

  // ── Mapping helpers ──
  const addMapping = () =>
    setMappings((prev) => [...prev, { fromProperty: '', toProperty: '', transformRule: 'value' }])

  const updateMapping = (idx: number, patch: Partial<MappingRow>) =>
    setMappings((prev) => prev.map((row, i) => (i === idx ? { ...row, ...patch } : row)))

  const removeMapping = (idx: number) =>
    setMappings((prev) => prev.filter((_, i) => i !== idx))

  // ── Save ──
  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      const validMappings: PropertyMapping[] = mappings
        .filter((m) => m.fromProperty && m.toProperty)
        .map((m) => ({
          fromProperty: m.fromProperty,
          toProperty: m.toProperty,
          transformRule: m.transformRule.trim() || 'value',
          fromUnit: getUnit(sourceProps, m.fromProperty),
          toUnit: getUnit(targetProps, m.toProperty),
        }))

      const updated = await updateRelationship(relationship.id, {
        relationshipType: relationshipType.trim(),
        properties: {},
        mappings: validMappings,
      })
      onSaved(updated)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!confirm('이 관계를 삭제할까요?')) return
    setSaveError(null)
    setDeleting(true)
    try {
      await deleteRelationship(relationship.id)
      onDeleted(relationship.id)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '삭제 실패')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
      <div className="assets-canvas-page__side-panel-header">
        <h3>관계 편집</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>

      <p style={{ fontSize: '0.78rem', color: '#888', marginBottom: '0.75rem' }}>
        {fromAssetType ?? relationship.fromAssetId} → {toAssetType ?? relationship.toAssetId}
      </p>

      <form onSubmit={handleSave}>
        {/* Relationship type */}
        <label style={{ display: 'block', fontSize: '0.8rem', marginBottom: '0.75rem' }}>
          관계 타입
          <select
            value={relationshipType}
            onChange={(e) => setRelationshipType(e.target.value)}
            style={{ display: 'block', width: '100%', marginTop: '0.25rem', padding: '0.35rem 0.5rem' }}
          >
            {typeOptions.map((opt) => (
              <option key={opt} value={opt}>
                {opt}
              </option>
            ))}
          </select>
        </label>

        {/* Mapping section */}
        <div style={{ marginTop: '0.25rem' }}>
          <span className="edit-rel__mapping-label">전파 속성 매핑</span>

          {sourceProps.length > 0 && targetProps.length > 0 && (
            <p className="create-rel__hint" style={{ marginBottom: '0.5rem' }}>
              규칙 예시: <code>value</code>, <code>value * 0.2</code>, <code>value / 3</code>
            </p>
          )}

          <div className="create-rel__mapping-list">
            {mappings.length === 0 && (
              <div className="create-rel__mapping-empty">매핑 없음</div>
            )}
            {mappings.map((row, idx) => {
              const fromUnit = getUnit(sourceProps, row.fromProperty)
              const toUnit = getUnit(targetProps, row.toProperty)
              const unitMismatch = !!(fromUnit && toUnit && fromUnit !== toUnit)
              return (
                <div key={idx} className="create-rel__mapping-row">
                  {/* Source property — dropdown if schema available, else free text */}
                  {sourceProps.length > 0 ? (
                    <select
                      value={row.fromProperty}
                      onChange={(e) => updateMapping(idx, { fromProperty: e.target.value })}
                      aria-label="source 속성"
                    >
                      <option value="">source 속성...</option>
                      {sourceProps.map((p) => (
                        <option key={p.key} value={p.key}>
                          {p.key}{p.unit ? ` (${p.unit})` : ''}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="text"
                      value={row.fromProperty}
                      onChange={(e) => updateMapping(idx, { fromProperty: e.target.value })}
                      placeholder="source 속성 키"
                    />
                  )}

                  <span className="create-rel__mapping-arrow">→</span>

                  {/* Target property */}
                  {targetProps.length > 0 ? (
                    <select
                      value={row.toProperty}
                      onChange={(e) => updateMapping(idx, { toProperty: e.target.value })}
                      aria-label="target 속성"
                    >
                      <option value="">target 속성...</option>
                      {targetProps.map((p) => (
                        <option key={p.key} value={p.key}>
                          {p.key}{p.unit ? ` (${p.unit})` : ''}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="text"
                      value={row.toProperty}
                      onChange={(e) => updateMapping(idx, { toProperty: e.target.value })}
                      placeholder="target 속성 키"
                    />
                  )}

                  <div className="create-rel__mapping-row-footer">
                    {fromUnit && <span className="create-rel__unit-badge">{fromUnit}</span>}
                    {unitMismatch && (
                      <span
                        className="create-rel__unit-warn"
                        title={`단위 불일치: ${fromUnit} ≠ ${toUnit}`}
                      >
                        ⚠️
                      </span>
                    )}
                    {toUnit && !(unitMismatch && fromUnit === toUnit) && (
                      <span className="create-rel__unit-badge">{toUnit}</span>
                    )}
                    <input
                      type="text"
                      value={row.transformRule}
                      onChange={(e) => updateMapping(idx, { transformRule: e.target.value })}
                      placeholder="value * 1.0"
                      aria-label="연산 규칙"
                    />
                    <button
                      type="button"
                      className="create-rel__mapping-delete"
                      onClick={() => removeMapping(idx)}
                      aria-label="매핑 삭제"
                    >
                      ✕
                    </button>
                  </div>
                </div>
              )
            })}
          </div>

          <button
            type="button"
            className="create-rel__add-mapping-btn"
            onClick={addMapping}
          >
            + 매핑 추가
          </button>
        </div>

        {saveError && (
          <p className="assets-canvas-page__error" style={{ marginTop: '0.5rem' }}>
            {saveError}
          </p>
        )}

        <div className="assets-canvas-page__panel-actions" style={{ marginTop: '1rem' }}>
          <button type="submit" disabled={saving}>
            {saving ? '저장 중…' : '저장'}
          </button>
          <button
            type="button"
            onClick={() => void handleDelete()}
            disabled={deleting}
            className="assets-canvas-page__delete-btn"
          >
            {deleting ? '삭제 중…' : '삭제'}
          </button>
        </div>
      </form>
    </>
  )
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  