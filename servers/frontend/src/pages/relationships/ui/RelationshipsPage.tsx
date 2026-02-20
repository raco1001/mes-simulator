import { useEffect, useState } from 'react'
import { getAssets, type AssetDto } from '@/entities/asset'
import {
  getRelationships,
  createRelationship,
  updateRelationship,
  deleteRelationship,
  type RelationshipDto,
  type CreateRelationshipRequest,
  type UpdateRelationshipRequest,
} from '@/entities/relationship'
import './RelationshipsPage.css'

export function RelationshipsPage() {
  const [relationships, setRelationships] = useState<RelationshipDto[]>([])
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formFromAssetId, setFormFromAssetId] = useState('')
  const [formToAssetId, setFormToAssetId] = useState('')
  const [formType, setFormType] = useState('')
  const [formPropertiesJson, setFormPropertiesJson] = useState('{}')
  const [createError, setCreateError] = useState<string | null>(null)
  const [editing, setEditing] = useState<RelationshipDto | null>(null)
  const [editFromAssetId, setEditFromAssetId] = useState('')
  const [editToAssetId, setEditToAssetId] = useState('')
  const [editType, setEditType] = useState('')
  const [editPropertiesJson, setEditPropertiesJson] = useState('{}')
  const [editError, setEditError] = useState<string | null>(null)

  const parseProperties = (json: string): Record<string, unknown> => {
    const trimmed = json.trim()
    if (!trimmed) return {}
    try {
      const parsed = JSON.parse(trimmed)
      return typeof parsed === 'object' && parsed !== null ? parsed : {}
    } catch {
      return {}
    }
  }

  const loadRelationships = async () => {
    try {
      const data = await getRelationships()
      setRelationships(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch relationships')
    }
  }

  const loadAssets = async () => {
    try {
      const data = await getAssets()
      setAssets(data)
    } catch {
      // non-fatal for this page
    }
  }

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      await Promise.all([loadRelationships(), loadAssets()])
      setLoading(false)
    }
    load()
  }, [])

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    if (!formFromAssetId.trim() || !formToAssetId.trim() || !formType.trim()) {
      setCreateError('From, To, Type are required')
      return
    }
    const body: CreateRelationshipRequest = {
      fromAssetId: formFromAssetId.trim(),
      toAssetId: formToAssetId.trim(),
      relationshipType: formType.trim(),
      properties: parseProperties(formPropertiesJson),
    }
    try {
      await createRelationship(body)
      setFormFromAssetId('')
      setFormToAssetId('')
      setFormType('')
      setFormPropertiesJson('{}')
      await loadRelationships()
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create relationship')
    }
  }

  const openEdit = (rel: RelationshipDto) => {
    setEditing(rel)
    setEditFromAssetId(rel.fromAssetId)
    setEditToAssetId(rel.toAssetId)
    setEditType(rel.relationshipType)
    setEditPropertiesJson(
      Object.keys(rel.properties ?? {}).length > 0
        ? JSON.stringify(rel.properties, null, 2)
        : '{}',
    )
    setEditError(null)
  }

  const closeEdit = () => {
    setEditing(null)
    setEditError(null)
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editing) return
    setEditError(null)
    const body: UpdateRelationshipRequest = {
      fromAssetId: editFromAssetId.trim() || undefined,
      toAssetId: editToAssetId.trim() || undefined,
      relationshipType: editType.trim() || undefined,
      properties: parseProperties(editPropertiesJson),
    }
    try {
      await updateRelationship(editing.id, body)
      closeEdit()
      await loadRelationships()
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update relationship')
    }
  }

  const handleDelete = async (id: string) => {
    if (!window.confirm('Delete this relationship?')) return
    try {
      await deleteRelationship(id)
      await loadRelationships()
      if (editing?.id === id) closeEdit()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete')
    }
  }

  const propertiesSummary = (p: Record<string, unknown> | undefined) => {
    if (!p || Object.keys(p).length === 0) return '-'
    return JSON.stringify(p)
  }

  if (loading) {
    return <div className="relationships-page-loading">Loading...</div>
  }

  return (
    <div className="relationships-page">
      <h1>관계</h1>

      <section className="relationships-page-section">
        <h2>관계 목록</h2>
        {error && <div className="relationships-page-error">{error}</div>}
        {relationships.length === 0 ? (
          <div className="relationships-page-empty">No relationships</div>
        ) : (
          <table className="relationships-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>From</th>
                <th>To</th>
                <th>Type</th>
                <th>Properties</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {relationships.map((rel) => (
                <tr key={rel.id}>
                  <td>{rel.id}</td>
                  <td>{rel.fromAssetId}</td>
                  <td>{rel.toAssetId}</td>
                  <td>{rel.relationshipType}</td>
                  <td className="relationships-properties">{propertiesSummary(rel.properties)}</td>
                  <td>
                    <button type="button" onClick={() => openEdit(rel)} className="relationships-edit-btn">
                      수정
                    </button>
                    <button type="button" onClick={() => handleDelete(rel.id)} className="relationships-delete-btn">
                      삭제
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="relationships-page-section">
        <h2>관계 생성</h2>
        <form onSubmit={handleCreateSubmit} className="relationships-form">
          <div className="form-group">
            <label htmlFor="rel-from">From Asset ID</label>
            <input
              id="rel-from"
              type="text"
              value={formFromAssetId}
              onChange={(e) => setFormFromAssetId(e.target.value)}
              placeholder="e.g. freezer-1"
              list="rel-from-list"
            />
            {assets.length > 0 && (
              <datalist id="rel-from-list">
                {assets.map((a) => (
                  <option key={a.id} value={a.id} />
                ))}
              </datalist>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="rel-to">To Asset ID</label>
            <input
              id="rel-to"
              type="text"
              value={formToAssetId}
              onChange={(e) => setFormToAssetId(e.target.value)}
              placeholder="e.g. conveyor-1"
              list="rel-to-list"
            />
            {assets.length > 0 && (
              <datalist id="rel-to-list">
                {assets.map((a) => (
                  <option key={a.id} value={a.id} />
                ))}
              </datalist>
            )}
          </div>
          <div className="form-group">
            <label htmlFor="rel-type">Relationship Type</label>
            <input
              id="rel-type"
              type="text"
              value={formType}
              onChange={(e) => setFormType(e.target.value)}
              placeholder="e.g. Supplies, Contains, ConnectedTo"
            />
          </div>
          <div className="form-group">
            <label htmlFor="rel-properties">Properties (JSON)</label>
            <textarea
              id="rel-properties"
              value={formPropertiesJson}
              onChange={(e) => setFormPropertiesJson(e.target.value)}
              placeholder='{"key": "value"}'
              rows={3}
            />
          </div>
          {createError && <div className="relationships-page-error">{createError}</div>}
          <button type="submit">생성</button>
        </form>
      </section>

      {editing && (
        <section className="relationships-page-section relationships-edit-modal">
          <h2>관계 수정 — {editing.id}</h2>
          <form onSubmit={handleEditSubmit} className="relationships-form">
            <div className="form-group">
              <label htmlFor="edit-from">From Asset ID</label>
              <input
                id="edit-from"
                type="text"
                value={editFromAssetId}
                onChange={(e) => setEditFromAssetId(e.target.value)}
                list="edit-from-list"
              />
              <datalist id="edit-from-list">
                {assets.map((a) => (
                  <option key={a.id} value={a.id} />
                ))}
              </datalist>
            </div>
            <div className="form-group">
              <label htmlFor="edit-to">To Asset ID</label>
              <input
                id="edit-to"
                type="text"
                value={editToAssetId}
                onChange={(e) => setEditToAssetId(e.target.value)}
                list="edit-to-list"
              />
              <datalist id="edit-to-list">
                {assets.map((a) => (
                  <option key={a.id} value={a.id} />
                ))}
              </datalist>
            </div>
            <div className="form-group">
              <label htmlFor="edit-type">Relationship Type</label>
              <input
                id="edit-type"
                type="text"
                value={editType}
                onChange={(e) => setEditType(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label htmlFor="edit-properties">Properties (JSON)</label>
              <textarea
                id="edit-properties"
                value={editPropertiesJson}
                onChange={(e) => setEditPropertiesJson(e.target.value)}
                rows={3}
              />
            </div>
            {editError && <div className="relationships-page-error">{editError}</div>}
            <div className="relationships-edit-actions">
              <button type="submit">저장</button>
              <button type="button" onClick={closeEdit}>
                취소
              </button>
            </div>
          </form>
        </section>
      )}
    </div>
  )
}
