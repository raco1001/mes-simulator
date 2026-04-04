import { useState } from 'react'
import {
  createObjectTypeSchema,
  updateObjectTypeSchema,
  deleteObjectTypeSchema,
  type CreateObjectTypeSchemaRequest,
  type DataType,
  type ObjectTypeSchemaDto,
  type ObjectTypeTraits,
  type PropertyDefinition,
  type SimulationBehavior,
  type UpdateObjectTypeSchemaRequest,
} from '@/entities/object-type-schema'
import { UnitSelect } from '@/shared/ui/UnitSelect'
import './EditTypeOnPanel.css'

const defaultObjectTraits = (): ObjectTypeTraits => ({
  persistence: 'Durable',
  dynamism: 'Dynamic',
  cardinality: 'Enumerable',
})

const emptyCreateObjectTypeRequest = (): CreateObjectTypeSchemaRequest => ({
  schemaVersion: 'v1',
  objectType: '',
  displayName: '',
  traits: defaultObjectTraits(),
  classifications: [],
  ownProperties: [],
  allowedLinks: [],
})

export function EditTypeOnPanel({
  schemas,
  onClose,
  onRefresh,
}: {
  schemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onRefresh: () => Promise<void>
}) {
  const [form, setForm] = useState<CreateObjectTypeSchemaRequest>(emptyCreateObjectTypeRequest)
  const [editingObjectType, setEditingObjectType] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const startCreate = () => {
    setEditingObjectType(null)
    setForm(emptyCreateObjectTypeRequest())
    setError(null)
  }

  const startEdit = (s: ObjectTypeSchemaDto) => {
    setEditingObjectType(s.objectType)
    setForm({
      schemaVersion: s.schemaVersion,
      objectType: s.objectType,
      displayName: s.displayName,
      traits: { ...s.traits },
      classifications: [...(s.classifications ?? [])],
      ownProperties: (s.ownProperties ?? []).map((p) => ({ ...p })),
      allowedLinks: [...(s.allowedLinks ?? [])],
    })
    setError(null)
  }

  const handleSave = async () => {
    setError(null)
    const ot = form.objectType.trim()
    const dn = form.displayName.trim()
    if (!ot || !dn) {
      setError('objectType과 displayName은 필수입니다.')
      return
    }
    setSaving(true)
    try {
      if (editingObjectType) {
        const body: UpdateObjectTypeSchemaRequest = {
          schemaVersion: form.schemaVersion,
          displayName: dn,
          traits: form.traits,
          classifications: form.classifications,
          ownProperties: form.ownProperties,
          allowedLinks: form.allowedLinks,
        }
        await updateObjectTypeSchema(editingObjectType, body)
      } else {
        await createObjectTypeSchema({
          ...form,
          objectType: ot,
          displayName: dn,
        })
      }
      await onRefresh()
      startCreate()
    } catch (err) {
      setError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (objectType: string) => {
    if (!window.confirm(`ObjectType "${objectType}" 스키마를 삭제할까요?`)) return
    setError(null)
    try {
      await deleteObjectTypeSchema(objectType)
      await onRefresh()
      if (editingObjectType === objectType) startCreate()
    } catch (err) {
      setError(err instanceof Error ? err.message : '삭제 실패')
    }
  }

  const addPropertyRow = () => {
    setForm((f) => ({
      ...f,
      ownProperties: [
        ...f.ownProperties,
        {
          key: '',
          dataType: 'Number',
          simulationBehavior: 'Settable',
          mutability: 'Mutable',
          required: true,
        },
      ],
    }))
  }

  const updatePropertyRow = (index: number, patch: Partial<PropertyDefinition>) => {
    setForm((f) => ({
      ...f,
      ownProperties: f.ownProperties.map((p, i) => (i === index ? { ...p, ...patch } : p)),
    }))
  }

  const removePropertyRow = (index: number) => {
    setForm((f) => ({
      ...f,
      ownProperties: f.ownProperties.filter((_, i) => i !== index),
    }))
  }

  return (
    <>
      <div className="assets-canvas-page__side-panel-header">
        <h3>ObjectType 관리</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>

      <div className="object-type-panel__list">
        <span className="object-type-panel__list-title">등록된 ObjectType</span>
        {schemas.length === 0 ? (
          <p className="object-type-panel__empty">스키마가 없습니다</p>
        ) : (
          <ul>
            {schemas.map((s) => (
              <li key={s.objectType}>
                <span className="object-type-panel__ot-name">{s.objectType}</span>
                <button type="button" onClick={() => startEdit(s)}>
                  수정
                </button>
                <button type="button" onClick={() => void handleDelete(s.objectType)}>
                  삭제
                </button>
              </li>
            ))}
          </ul>
        )}
        <button type="button" className="object-type-panel__new-btn" onClick={startCreate}>
          + 새 ObjectType
        </button>
      </div>

      <div className="object-type-panel__form">
        <span className="object-type-panel__form-title">
          {editingObjectType ? `수정: ${editingObjectType}` : '새 ObjectType'}
        </span>
        <label>
          schemaVersion
          <input
            value={form.schemaVersion}
            onChange={(e) => setForm((f) => ({ ...f, schemaVersion: e.target.value }))}
          />
        </label>
        <label>
          objectType
          <input
            value={form.objectType}
            onChange={(e) => setForm((f) => ({ ...f, objectType: e.target.value }))}
            disabled={editingObjectType != null}
            required
          />
        </label>
        <label>
          displayName
          <input
            value={form.displayName}
            onChange={(e) => setForm((f) => ({ ...f, displayName: e.target.value }))}
            required
          />
        </label>
        <div className="object-type-panel__traits">
          <label>
            persistence
            <select
              value={form.traits.persistence}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  traits: { ...f.traits, persistence: e.target.value as ObjectTypeTraits['persistence'] },
                }))
              }
            >
              <option value="Permanent">Permanent</option>
              <option value="Durable">Durable</option>
              <option value="Transient">Transient</option>
            </select>
          </label>
          <label>
            dynamism
            <select
              value={form.traits.dynamism}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  traits: { ...f.traits, dynamism: e.target.value as ObjectTypeTraits['dynamism'] },
                }))
              }
            >
              <option value="Static">Static</option>
              <option value="Dynamic">Dynamic</option>
              <option value="Reactive">Reactive</option>
            </select>
          </label>
          <label>
            cardinality
            <select
              value={form.traits.cardinality}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  traits: { ...f.traits, cardinality: e.target.value as ObjectTypeTraits['cardinality'] },
                }))
              }
            >
              <option value="Singular">Singular</option>
              <option value="Enumerable">Enumerable</option>
              <option value="Streaming">Streaming</option>
            </select>
          </label>
        </div>

        <div className="object-type-panel__props">
          <span>ownProperties</span>
          {form.ownProperties.map((p, i) => (
            <div key={i} className="object-type-panel__prop-row">
              <input
                placeholder="key"
                value={p.key}
                onChange={(e) => updatePropertyRow(i, { key: e.target.value })}
              />
              <select
                value={p.dataType}
                onChange={(e) => updatePropertyRow(i, { dataType: e.target.value as DataType })}
              >
                {(['Number', 'String', 'Boolean', 'DateTime', 'Array', 'Object'] as const).map((dt) => (
                  <option key={dt} value={dt}>
                    {dt}
                  </option>
                ))}
              </select>
              <select
                value={p.simulationBehavior}
                onChange={(e) =>
                  updatePropertyRow(i, {
                    simulationBehavior: e.target.value as SimulationBehavior,
                  })
                }
              >
                {(['Constant', 'Settable', 'Rate', 'Accumulator', 'Derived'] as const).map((sb) => (
                  <option key={sb} value={sb}>
                    {sb}
                  </option>
                ))}
              </select>
              <select
                value={p.mutability}
                onChange={(e) =>
                  updatePropertyRow(i, { mutability: e.target.value as 'Immutable' | 'Mutable' })
                }
              >
                <option value="Immutable">Immutable</option>
                <option value="Mutable">Mutable</option>
              </select>
              {p.dataType === 'Number' && (
                <div className="object-type-panel__prop-unit">
                  <UnitSelect
                    value={p.unit}
                    onChange={(unit) => updatePropertyRow(i, { unit: unit || undefined })}
                  />
                </div>
              )}
              <label className="object-type-panel__prop-required">
                <input
                  type="checkbox"
                  checked={p.required}
                  onChange={(e) => updatePropertyRow(i, { required: e.target.checked })}
                />
                required
              </label>
              <button type="button" onClick={() => removePropertyRow(i)}>
                제거
              </button>
            </div>
          ))}
          <button type="button" onClick={addPropertyRow}>
            + 속성 추가
          </button>
        </div>

        {error && <p className="assets-canvas-page__error">{error}</p>}
        <button type="button" className="object-type-panel__save-btn" disabled={saving} onClick={() => void handleSave()}>
          {saving ? '저장 중…' : '저장'}
        </button>
      </div>
    </>
  )
}
