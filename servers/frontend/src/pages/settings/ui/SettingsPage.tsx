import { useEffect, useState } from 'react'
import {
  getAssets,
  createAsset,
  updateAsset,
  type AssetDto,
  type CreateAssetRequest,
  type UpdateAssetRequest,
} from '@/entities/asset'
import {
  getRelationships,
  createRelationship,
  updateRelationship,
  deleteRelationship,
  type RelationshipDto,
  type CreateRelationshipRequest,
  type UpdateRelationshipRequest,
} from '@/entities/relationship'
import './SettingsPage.css'

type Tab = 'assets' | 'relationships'

function recordToMetadataRows(meta: Record<string, unknown>): Array<{ key: string; value: string }> {
  return Object.entries(meta ?? {}).map(([key, value]) => ({
    key,
    value: typeof value === 'string' ? value : JSON.stringify(value),
  }))
}

function formMetadataToRecord(rows: Array<{ key: string; value: string }>): Record<string, unknown> {
  const out: Record<string, unknown> = {}
  for (const row of rows) {
    const k = row.key.trim()
    if (k) out[k] = row.value
  }
  return out
}

function parseProperties(json: string): Record<string, unknown> {
  const trimmed = json.trim()
  if (!trimmed) return {}
  try {
    const parsed = JSON.parse(trimmed)
    return typeof parsed === 'object' && parsed !== null ? parsed : {}
  } catch {
    return {}
  }
}

export function SettingsPage() {
  const [tab, setTab] = useState<Tab>('assets')

  return (
    <div className="settings-page">
      <h1>설정</h1>
      <div className="settings-tabs">
        <button
          type="button"
          className={`settings-tab ${tab === 'assets' ? 'settings-tab--active' : ''}`}
          onClick={() => setTab('assets')}
        >
          에셋
        </button>
        <button
          type="button"
          className={`settings-tab ${tab === 'relationships' ? 'settings-tab--active' : ''}`}
          onClick={() => setTab('relationships')}
        >
          관계
        </button>
      </div>
      {tab === 'assets' ? <AssetsTab /> : <RelationshipsTab />}
    </div>
  )
}

function AssetsTab() {
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formType, setFormType] = useState('')
  const [formConnections, setFormConnections] = useState('')
  const [formMetadata, setFormMetadata] = useState<Array<{ key: string; value: string }>>([])
  const [createError, setCreateError] = useState<string | null>(null)
  const [editingAsset, setEditingAsset] = useState<AssetDto | null>(null)
  const [editType, setEditType] = useState('')
  const [editConnections, setEditConnections] = useState('')
  const [editMetadata, setEditMetadata] = useState<Array<{ key: string; value: string }>>([])
  const [editError, setEditError] = useState<string | null>(null)

  const loadAssets = async () => {
    try {
      setLoading(true)
      const data = await getAssets()
      setAssets(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch assets')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadAssets() }, [])

  const metadataSummary = (meta: Record<string, unknown> | undefined) => {
    if (!meta || Object.keys(meta).length === 0) return '-'
    return Object.entries(meta).slice(0, 2).map(([k, v]) => `${k}: ${v}`).join(', ')
  }

  const addMetadataRow = () => setFormMetadata((prev) => [...prev, { key: '', value: '' }])
  const removeMetadataRow = (i: number) => setFormMetadata((prev) => prev.filter((_, idx) => idx !== i))
  const setMetadataRow = (i: number, field: 'key' | 'value', value: string) =>
    setFormMetadata((prev) => prev.map((row, idx) => (idx === i ? { ...row, [field]: value } : row)))

  const addEditMetadataRow = () => setEditMetadata((prev) => [...prev, { key: '', value: '' }])
  const removeEditMetadataRow = (i: number) => setEditMetadata((prev) => prev.filter((_, idx) => idx !== i))
  const setEditMetadataRow = (i: number, field: 'key' | 'value', value: string) =>
    setEditMetadata((prev) => prev.map((row, idx) => (idx === i ? { ...row, [field]: value } : row)))

  const openEdit = (asset: AssetDto) => {
    setEditingAsset(asset)
    setEditType(asset.type)
    setEditConnections(asset.connections?.length ? asset.connections.join(', ') : '')
    setEditMetadata(recordToMetadataRows(asset.metadata ?? {}))
    setEditError(null)
  }

  const closeEdit = () => { setEditingAsset(null); setEditError(null) }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const type = formType.trim()
    if (!type) { setCreateError('Type is required'); return }
    const connections = formConnections.split(',').map((s) => s.trim()).filter(Boolean)
    const metadata = formMetadataToRecord(formMetadata)
    const body: CreateAssetRequest = { type, connections, metadata }
    try {
      await createAsset(body)
      setFormType(''); setFormConnections(''); setFormMetadata([])
      await loadAssets()
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create')
    }
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingAsset) return
    setEditError(null)
    const connections = editConnections.split(',').map((s) => s.trim()).filter(Boolean)
    const metadata = formMetadataToRecord(editMetadata)
    const body: UpdateAssetRequest = {
      type: editType.trim() || undefined,
      connections,
      metadata: Object.keys(metadata).length ? metadata : undefined,
    }
    try {
      await updateAsset(editingAsset.id, body)
      closeEdit()
      await loadAssets()
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update')
    }
  }

  if (loading) return <div className="settings-loading">Loading assets...</div>

  return (
    <>
      {editingAsset && (
        <section className="settings-edit-section">
          <h2>에셋 수정 — {editingAsset.id}</h2>
          <form onSubmit={handleEditSubmit} className="settings-form">
            <div className="form-group">
              <label htmlFor="edit-type">Type</label>
              <input id="edit-type" type="text" value={editType} onChange={(e) => setEditType(e.target.value)} />
            </div>
            <div className="form-group">
              <label htmlFor="edit-connections">Connections (쉼표 구분)</label>
              <input id="edit-connections" type="text" value={editConnections} onChange={(e) => setEditConnections(e.target.value)} />
            </div>
            <div className="form-group">
              <label>Metadata</label>
              {editMetadata.map((row, i) => (
                <div key={i} className="metadata-row">
                  <input type="text" value={row.key} onChange={(e) => setEditMetadataRow(i, 'key', e.target.value)} placeholder="key" />
                  <input type="text" value={row.value} onChange={(e) => setEditMetadataRow(i, 'value', e.target.value)} placeholder="value" />
                  <button type="button" onClick={() => removeEditMetadataRow(i)} className="metadata-remove">삭제</button>
                </div>
              ))}
              <button type="button" onClick={addEditMetadataRow} className="metadata-add">항목 추가</button>
            </div>
            {editError && <div className="settings-error">{editError}</div>}
            <div className="settings-edit-actions">
              <button type="submit">저장</button>
              <button type="button" onClick={closeEdit}>취소</button>
            </div>
          </form>
        </section>
      )}

      <section className="settings-section">
        <h2>에셋 목록</h2>
        {error && <div className="settings-error">{error}</div>}
        {assets.length === 0 ? (
          <div className="settings-empty">No assets found</div>
        ) : (
          <table className="settings-table">
            <thead>
              <tr><th>ID</th><th>Type</th><th>Connections</th><th>Metadata</th><th>Created</th><th></th></tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id}>
                  <td>{asset.id}</td>
                  <td>{asset.type}</td>
                  <td>{asset.connections?.length ? asset.connections.join(', ') : '-'}</td>
                  <td>{metadataSummary(asset.metadata)}</td>
                  <td>{new Date(asset.createdAt).toLocaleString()}</td>
                  <td><button type="button" onClick={() => openEdit(asset)} className="settings-edit-btn">수정</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="settings-section">
        <h2>에셋 생성</h2>
        <form onSubmit={handleCreate} className="settings-form">
          <div className="form-group">
            <label htmlFor="asset-type">Type (필수)</label>
            <input id="asset-type" type="text" value={formType} onChange={(e) => setFormType(e.target.value)} placeholder="e.g. freezer, conveyor" />
          </div>
          <div className="form-group">
            <label htmlFor="asset-connections">Connections (쉼표 구분)</label>
            <input id="asset-connections" type="text" value={formConnections} onChange={(e) => setFormConnections(e.target.value)} placeholder="id1, id2" />
          </div>
          <div className="form-group">
            <label>Metadata (선택)</label>
            {formMetadata.map((row, i) => (
              <div key={i} className="metadata-row">
                <input type="text" value={row.key} onChange={(e) => setMetadataRow(i, 'key', e.target.value)} placeholder="key" />
                <input type="text" value={row.value} onChange={(e) => setMetadataRow(i, 'value', e.target.value)} placeholder="value" />
                <button type="button" onClick={() => removeMetadataRow(i)} className="metadata-remove">삭제</button>
              </div>
            ))}
            <button type="button" onClick={addMetadataRow} className="metadata-add">항목 추가</button>
          </div>
          {createError && <div className="settings-error">{createError}</div>}
          <button type="submit">생성</button>
        </form>
      </section>
    </>
  )
}

function RelationshipsTab() {
  const [relationships, setRelationships] = useState<RelationshipDto[]>([])
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formFrom, setFormFrom] = useState('')
  const [formTo, setFormTo] = useState('')
  const [formType, setFormType] = useState('')
  const [formProps, setFormProps] = useState('{}')
  const [createError, setCreateError] = useState<string | null>(null)
  const [editing, setEditing] = useState<RelationshipDto | null>(null)
  const [editFrom, setEditFrom] = useState('')
  const [editTo, setEditTo] = useState('')
  const [editType, setEditType] = useState('')
  const [editProps, setEditProps] = useState('{}')
  const [editError, setEditError] = useState<string | null>(null)

  const loadData = async () => {
    setLoading(true)
    await Promise.all([
      getRelationships().then(setRelationships).catch(() => {}),
      getAssets().then(setAssets).catch(() => {}),
    ])
    setLoading(false)
  }

  useEffect(() => { loadData() }, [])

  const openEdit = (rel: RelationshipDto) => {
    setEditing(rel)
    setEditFrom(rel.fromAssetId)
    setEditTo(rel.toAssetId)
    setEditType(rel.relationshipType)
    setEditProps(Object.keys(rel.properties ?? {}).length > 0 ? JSON.stringify(rel.properties, null, 2) : '{}')
    setEditError(null)
  }

  const closeEdit = () => { setEditing(null); setEditError(null) }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    if (!formFrom.trim() || !formTo.trim() || !formType.trim()) {
      setCreateError('From, To, Type are required')
      return
    }
    const body: CreateRelationshipRequest = {
      fromAssetId: formFrom.trim(),
      toAssetId: formTo.trim(),
      relationshipType: formType.trim(),
      properties: parseProperties(formProps),
    }
    try {
      await createRelationship(body)
      setFormFrom(''); setFormTo(''); setFormType(''); setFormProps('{}')
      const data = await getRelationships()
      setRelationships(data)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create')
    }
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editing) return
    setEditError(null)
    const body: UpdateRelationshipRequest = {
      fromAssetId: editFrom.trim() || undefined,
      toAssetId: editTo.trim() || undefined,
      relationshipType: editType.trim() || undefined,
      properties: parseProperties(editProps),
    }
    try {
      await updateRelationship(editing.id, body)
      closeEdit()
      const data = await getRelationships()
      setRelationships(data)
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update')
    }
  }

  const handleDelete = async (id: string) => {
    if (!window.confirm('Delete this relationship?')) return
    try {
      await deleteRelationship(id)
      const data = await getRelationships()
      setRelationships(data)
      if (editing?.id === id) closeEdit()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete')
    }
  }

  const propsSummary = (p: Record<string, unknown> | undefined) => {
    if (!p || Object.keys(p).length === 0) return '-'
    return JSON.stringify(p)
  }

  if (loading) return <div className="settings-loading">Loading relationships...</div>

  return (
    <>
      {editing && (
        <section className="settings-edit-section">
          <h2>관계 수정 — {editing.id}</h2>
          <form onSubmit={handleEditSubmit} className="settings-form">
            <div className="form-group">
              <label htmlFor="edit-from">From Asset ID</label>
              <input id="edit-from" type="text" value={editFrom} onChange={(e) => setEditFrom(e.target.value)} list="edit-from-list" />
              <datalist id="edit-from-list">{assets.map((a) => <option key={a.id} value={a.id} />)}</datalist>
            </div>
            <div className="form-group">
              <label htmlFor="edit-to">To Asset ID</label>
              <input id="edit-to" type="text" value={editTo} onChange={(e) => setEditTo(e.target.value)} list="edit-to-list" />
              <datalist id="edit-to-list">{assets.map((a) => <option key={a.id} value={a.id} />)}</datalist>
            </div>
            <div className="form-group">
              <label htmlFor="edit-rel-type">Relationship Type</label>
              <input id="edit-rel-type" type="text" value={editType} onChange={(e) => setEditType(e.target.value)} />
            </div>
            <div className="form-group">
              <label htmlFor="edit-props">Properties (JSON)</label>
              <textarea id="edit-props" value={editProps} onChange={(e) => setEditProps(e.target.value)} rows={3} />
            </div>
            {editError && <div className="settings-error">{editError}</div>}
            <div className="settings-edit-actions">
              <button type="submit">저장</button>
              <button type="button" onClick={closeEdit}>취소</button>
            </div>
          </form>
        </section>
      )}

      <section className="settings-section">
        <h2>관계 목록</h2>
        {error && <div className="settings-error">{error}</div>}
        {relationships.length === 0 ? (
          <div className="settings-empty">No relationships</div>
        ) : (
          <table className="settings-table">
            <thead>
              <tr><th>ID</th><th>From</th><th>To</th><th>Type</th><th>Properties</th><th></th></tr>
            </thead>
            <tbody>
              {relationships.map((rel) => (
                <tr key={rel.id}>
                  <td>{rel.id}</td>
                  <td>{rel.fromAssetId}</td>
                  <td>{rel.toAssetId}</td>
                  <td>{rel.relationshipType}</td>
                  <td className="settings-properties">{propsSummary(rel.properties)}</td>
                  <td>
                    <button type="button" onClick={() => openEdit(rel)} className="settings-edit-btn">수정</button>
                    <button type="button" onClick={() => handleDelete(rel.id)} className="settings-delete-btn">삭제</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="settings-section">
        <h2>관계 생성</h2>
        <form onSubmit={handleCreate} className="settings-form">
          <div className="form-group">
            <label htmlFor="rel-from">From Asset ID</label>
            <input id="rel-from" type="text" value={formFrom} onChange={(e) => setFormFrom(e.target.value)} placeholder="e.g. freezer-1" list="rel-from-list" />
            {assets.length > 0 && <datalist id="rel-from-list">{assets.map((a) => <option key={a.id} value={a.id} />)}</datalist>}
          </div>
          <div className="form-group">
            <label htmlFor="rel-to">To Asset ID</label>
            <input id="rel-to" type="text" value={formTo} onChange={(e) => setFormTo(e.target.value)} placeholder="e.g. conveyor-1" list="rel-to-list" />
            {assets.length > 0 && <datalist id="rel-to-list">{assets.map((a) => <option key={a.id} value={a.id} />)}</datalist>}
          </div>
          <div className="form-group">
            <label htmlFor="rel-type">Relationship Type</label>
            <input id="rel-type" type="text" value={formType} onChange={(e) => setFormType(e.target.value)} placeholder="e.g. feeds_into" />
          </div>
          <div className="form-group">
            <label htmlFor="rel-props">Properties (JSON)</label>
            <textarea id="rel-props" value={formProps} onChange={(e) => setFormProps(e.target.value)} placeholder='{"key": "value"}' rows={3} />
          </div>
          {createError && <div className="settings-error">{createError}</div>}
          <button type="submit">생성</button>
        </form>
      </section>
    </>
  )
}
