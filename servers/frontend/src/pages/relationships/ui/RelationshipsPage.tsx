import { useEffect, useState, type Dispatch, type FormEvent, type SetStateAction } from 'react'
import { getAssets, type AssetDto } from '@/entities/asset'
import { getObjectTypeSchema, type ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import {
  getRelationships,
  createRelationship,
  updateRelationship,
  deleteRelationship,
  type RelationshipDto,
  type CreateRelationshipRequest,
  type UpdateRelationshipRequest,
  type PropertyMapping,
} from '@/entities/relationship'
import './RelationshipsPage.css'

const emptyMappingRow = (): PropertyMapping => ({
  fromProperty: '',
  toProperty: '',
  transformRule: 'value',
})

function filterValidMappings(mappings: PropertyMapping[]): PropertyMapping[] {
  return mappings
    .filter((m) => m.fromProperty.trim() && m.toProperty.trim())
    .map((m) => ({
      fromProperty: m.fromProperty.trim(),
      toProperty: m.toProperty.trim(),
      transformRule: (m.transformRule?.trim() || 'value'),
      fromUnit: m.fromUnit ?? undefined,
      toUnit: m.toUnit ?? undefined,
    }))
}

function RelationshipMappingsFields({
  fromSchema,
  toSchema,
  mappings,
  setMappings,
}: {
  fromSchema: ObjectTypeSchemaDto | null
  toSchema: ObjectTypeSchemaDto | null
  mappings: PropertyMapping[]
  setMappings: Dispatch<SetStateAction<PropertyMapping[]>>
}) {
  if (!fromSchema || !toSchema) return null

  const fromProps = fromSchema.ownProperties ?? []
  const toProps = toSchema.ownProperties ?? []

  return (
    <div className="relationships-mappings-block">
      <div className="relationships-mappings-title">속성 매핑</div>
      {mappings.map((m, idx) => {
        const fromProp = fromProps.find((p) => p.key === m.fromProperty)
        const toProp = toProps.find((p) => p.key === m.toProperty)
        const unitMismatch =
          Boolean(fromProp?.unit && toProp?.unit && fromProp.unit !== toProp.unit)

        return (
          <div key={idx} className="relationships-mappings-row">
            <select
              value={m.fromProperty}
              onChange={(e) => {
                const fp = fromProps.find((p) => p.key === e.target.value)
                setMappings((prev) => {
                  const next = [...prev]
                  next[idx] = {
                    ...m,
                    fromProperty: e.target.value,
                    fromUnit: fp?.unit,
                  }
                  return next
                })
              }}
              aria-label="Source property"
            >
              <option value="">source 속성</option>
              {fromProps.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.key}
                  {p.unit ? ` (${p.unit})` : ''}
                </option>
              ))}
            </select>
            <span className="relationships-mappings-arrow" aria-hidden>
              →
            </span>
            <select
              value={m.toProperty}
              onChange={(e) => {
                const tp = toProps.find((p) => p.key === e.target.value)
                setMappings((prev) => {
                  const next = [...prev]
                  next[idx] = {
                    ...m,
                    toProperty: e.target.value,
                    toUnit: tp?.unit,
                  }
                  return next
                })
              }}
              aria-label="Target property"
            >
              <option value="">target 속성</option>
              {toProps.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.key}
                  {p.unit ? ` (${p.unit})` : ''}
                </option>
              ))}
            </select>
            <input
              value={m.transformRule ?? 'value'}
              onChange={(e) => {
                setMappings((prev) => {
                  const next = [...prev]
                  next[idx] = { ...m, transformRule: e.target.value }
                  return next
                })
              }}
              placeholder="value * 1.0"
              className="relationships-mappings-transform"
            />
            {unitMismatch && (
              <span
                className="relationships-mappings-warn"
                title={`단위 불일치: ${fromProp?.unit} ≠ ${toProp?.unit}`}
              >
                (warn)
              </span>
            )}
            <button
              type="button"
              className="relationships-mappings-remove"
              onClick={() => setMappings((prev) => prev.filter((_, i) => i !== idx))}
            >
              제거
            </button>
          </div>
        )
      })}
      <button
        type="button"
        className="relationships-mappings-add"
        onClick={() => setMappings((prev) => [...prev, emptyMappingRow()])}
      >
        + 매핑 추가
      </button>
    </div>
  )
}

function mappingsSummary(rel: RelationshipDto): string {
  const n = rel.mappings?.length ?? 0
  return n > 0 ? `${n} mappings` : '—'
}

export function RelationshipsPage() {
  const [relationships, setRelationships] = useState<RelationshipDto[]>([])
  const [assets, setAssets] = useState<AssetDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [formFromAssetId, setFormFromAssetId] = useState('')
  const [formToAssetId, setFormToAssetId] = useState('')
  const [formType, setFormType] = useState('')
  const [formPropertiesJson, setFormPropertiesJson] = useState('{}')
  const [formMappings, setFormMappings] = useState<PropertyMapping[]>([])
  const [fromSchema, setFromSchema] = useState<ObjectTypeSchemaDto | null>(null)
  const [toSchema, setToSchema] = useState<ObjectTypeSchemaDto | null>(null)
  const [createError, setCreateError] = useState<string | null>(null)
  const [editing, setEditing] = useState<RelationshipDto | null>(null)
  const [editFromAssetId, setEditFromAssetId] = useState('')
  const [editToAssetId, setEditToAssetId] = useState('')
  const [editType, setEditType] = useState('')
  const [editPropertiesJson, setEditPropertiesJson] = useState('{}')
  const [editMappings, setEditMappings] = useState<PropertyMapping[]>([])
  const [editFromSchema, setEditFromSchema] = useState<ObjectTypeSchemaDto | null>(null)
  const [editToSchema, setEditToSchema] = useState<ObjectTypeSchemaDto | null>(null)
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
    void load()
  }, [])

  useEffect(() => {
    const id = formFromAssetId.trim()
    if (!id) {
      setFromSchema(null)
      return
    }
    const asset = assets.find((a) => a.id === id)
    if (!asset?.type) {
      setFromSchema(null)
      return
    }
    let cancelled = false
    void getObjectTypeSchema(asset.type)
      .then((s) => {
        if (!cancelled) setFromSchema(s)
      })
      .catch(() => {
        if (!cancelled) setFromSchema(null)
      })
    return () => {
      cancelled = true
    }
  }, [formFromAssetId, assets])

  useEffect(() => {
    const id = formToAssetId.trim()
    if (!id) {
      setToSchema(null)
      return
    }
    const asset = assets.find((a) => a.id === id)
    if (!asset?.type) {
      setToSchema(null)
      return
    }
    let cancelled = false
    void getObjectTypeSchema(asset.type)
      .then((s) => {
        if (!cancelled) setToSchema(s)
      })
      .catch(() => {
        if (!cancelled) setToSchema(null)
      })
    return () => {
      cancelled = true
    }
  }, [formToAssetId, assets])

  useEffect(() => {
    if (!editing) {
      setEditFromSchema(null)
      setEditToSchema(null)
      return
    }
    const fromId = editFromAssetId.trim()
    const fromAsset = assets.find((a) => a.id === fromId)
    if (!fromAsset?.type) {
      setEditFromSchema(null)
    } else {
      let cancelled = false
      void getObjectTypeSchema(fromAsset.type)
        .then((s) => {
          if (!cancelled) setEditFromSchema(s)
        })
        .catch(() => {
          if (!cancelled) setEditFromSchema(null)
        })
      return () => {
        cancelled = true
      }
    }
    return undefined
  }, [editing, editFromAssetId, assets])

  useEffect(() => {
    if (!editing) return undefined
    const toId = editToAssetId.trim()
    const toAsset = assets.find((a) => a.id === toId)
    if (!toAsset?.type) {
      setEditToSchema(null)
      return undefined
    }
    let cancelled = false
    void getObjectTypeSchema(toAsset.type)
      .then((s) => {
        if (!cancelled) setEditToSchema(s)
      })
      .catch(() => {
        if (!cancelled) setEditToSchema(null)
      })
    return () => {
      cancelled = true
    }
  }, [editing, editToAssetId, assets])

  const handleCreateSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    if (!formFromAssetId.trim() || !formToAssetId.trim() || !formType.trim()) {
      setCreateError('From, To, Type are required')
      return
    }
    const validMappings = filterValidMappings(formMappings)
    const body: CreateRelationshipRequest = {
      fromAssetId: formFromAssetId.trim(),
      toAssetId: formToAssetId.trim(),
      relationshipType: formType.trim(),
      properties: parseProperties(formPropertiesJson),
      ...(validMappings.length > 0 ? { mappings: validMappings } : {}),
    }
    try {
      await createRelationship(body)
      setFormFromAssetId('')
      setFormToAssetId('')
      setFormType('')
      setFormPropertiesJson('{}')
      setFormMappings([])
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
    setEditMappings(
      (rel.mappings ?? []).map((m) => ({
        fromProperty: m.fromProperty,
        toProperty: m.toProperty,
        transformRule: m.transformRule ?? 'value',
        fromUnit: m.fromUnit,
        toUnit: m.toUnit,
      })),
    )
    setEditError(null)
  }

  const closeEdit = () => {
    setEditing(null)
    setEditError(null)
    setEditFromSchema(null)
    setEditToSchema(null)
  }

  const handleEditSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!editing) return
    setEditError(null)
    const validMappings = filterValidMappings(editMappings)
    const body: UpdateRelationshipRequest = {
      fromAssetId: editFromAssetId.trim() || undefined,
      toAssetId: editToAssetId.trim() || undefined,
      relationshipType: editType.trim() || undefined,
      properties: parseProperties(editPropertiesJson),
      mappings: validMappings,
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
                <th>Mappings</th>
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
                  <td>{mappingsSummary(rel)}</td>
                  <td>
                    <button type="button" onClick={() => openEdit(rel)} className="relationships-edit-btn">
                      수정
                    </button>
                    <button type="button" onClick={() => void handleDelete(rel.id)} className="relationships-delete-btn">
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
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="relationships-form">
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
          <RelationshipMappingsFields
            fromSchema={fromSchema}
            toSchema={toSchema}
            mappings={formMappings}
            setMappings={setFormMappings}
          />
          {createError && <div className="relationships-page-error">{createError}</div>}
          <button type="submit">생성</button>
        </form>
      </section>

      {editing && (
        <section className="relationships-page-section relationships-edit-modal">
          <h2>관계 수정 — {editing.id}</h2>
          <form onSubmit={(e) => void handleEditSubmit(e)} className="relationships-form">
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
            <RelationshipMappingsFields
              fromSchema={editFromSchema}
              toSchema={editToSchema}
              mappings={editMappings}
              setMappings={setEditMappings}
            />
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
