import { useState, type FormEvent } from 'react'
import { updateRelationship, deleteRelationship, type RelationshipDto } from '@/entities/relationship'
import type { LinkTypeSchemaDto } from '@/entities/link-type-schema'

export function EditRelationshipOnPanel({
  relationship,
  linkTypeSchemas,
  onClose,
  onSaved,
  onDeleted,
}: {
  relationship: RelationshipDto
  linkTypeSchemas: LinkTypeSchemaDto[]
  onClose: () => void
  onSaved: (updated: RelationshipDto) => void
  onDeleted: (id: string) => void
}) {
  const [relationshipType, setRelationshipType] = useState(relationship.relationshipType)
  const [propertiesJson, setPropertiesJson] = useState(() =>
    JSON.stringify(relationship.properties ?? {}, null, 2),
  )
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  const typeOptions = linkTypeSchemas.length > 0
    ? linkTypeSchemas.map((s) => s.linkType)
    : [relationship.relationshipType]

  const parseProps = (): Record<string, unknown> => {
    const t = propertiesJson.trim()
    if (!t) return {}
    try {
      const p = JSON.parse(t)
      return typeof p === 'object' && p !== null ? p : {}
    } catch {
      return {}
    }
  }

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      const updated = await updateRelationship(relationship.id, {
        relationshipType: relationshipType.trim(),
        properties: parseProps(),
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
      <p className="assets-canvas-page__rel-hint">
        From: {relationship.fromAssetId} → To: {relationship.toAssetId}
      </p>
      <form onSubmit={handleSave}>
        <label>
          관계 타입
          <select value={relationshipType} onChange={(e) => setRelationshipType(e.target.value)}>
            {typeOptions.map((opt) => (
              <option key={opt} value={opt}>
                {opt}
              </option>
            ))}
          </select>
        </label>
        <label>
          Properties (JSON)
          <textarea
            value={propertiesJson}
            onChange={(e) => setPropertiesJson(e.target.value)}
            rows={3}
            placeholder='{"capacity": 1000}'
          />
        </label>
        {saveError && <p className="assets-canvas-page__error">{saveError}</p>}
        <div className="assets-canvas-page__panel-actions">
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
