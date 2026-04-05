import { useEffect, useMemo, useState } from 'react'
import type { Node } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'
import {
  createRelationship,
  deleteRelationship,
  updateRelationship,
  type CreateRelationshipRequest,
  type PropertyMapping,
  type RelationshipDto,
} from '@/entities/relationship'
import type { LinkTypeSchemaDto } from '@/entities/link-type-schema'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { mergeNumberMappingProperties } from '@/shared/lib/canvasMetadata'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
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

/** Matches canvas flow node `data` shape (see pages/canvas AssetNode). */
export type CreateRelationshipFlowNodeData = {
  asset: AssetDto
  liveStatus?: string
  liveProperties?: Record<string, unknown>
}

export function CreateRelationshipOnPanel({
  editingRelationship = null,
  sourceId,
  targetId,
  nodes,
  linkTypeSchemas,
  objectTypeSchemas,
  onSetSource,
  onSetTarget,
  onSwap,
  onClose,
  onCreated,
  onSaved,
  onDeleted,
}: {
  editingRelationship?: RelationshipDto | null
  sourceId: string | null
  targetId: string | null
  nodes: Node<CreateRelationshipFlowNodeData>[]
  linkTypeSchemas: LinkTypeSchemaDto[]
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onSetSource: (id: string | null) => void
  onSetTarget: (id: string | null) => void
  onSwap: () => void
  onClose: () => void
  /** Called after a new relationship is created (create mode only). */
  onCreated?: () => void
  /** Called after update (edit mode only). */
  onSaved?: (updated: RelationshipDto) => void
  /** Called after delete (edit mode only). */
  onDeleted?: (id: string) => void
}) {
  const isEdit = editingRelationship != null
  const lockAssets = isEdit

  const [selectedLinkType, setSelectedLinkType] = useState('')
  const [mappings, setMappings] = useState<MappingRow[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (!editingRelationship) {
      setSelectedLinkType('')
      setMappings([])
      setSubmitError(null)
      return
    }
    setSelectedLinkType(editingRelationship.relationshipType)
    setMappings(toRows(editingRelationship.mappings))
    setSubmitError(null)
  }, [editingRelationship])

  // ── Source ──
  const sourceNode = sourceId ? nodes.find((n) => n.id === sourceId) : null
  const sourceAssetType = sourceNode?.data.asset.type ?? null
  const sourceSchema = sourceAssetType
    ? objectTypeSchemas.find((s) => s.objectType === sourceAssetType) ?? null
    : null
  const sourceEligibleProps = useMemo(
    () =>
      mergeNumberMappingProperties(
        sourceSchema,
        sourceNode?.data.asset.metadata,
      ),
    [sourceSchema, sourceNode?.data.asset.metadata],
  )

  // ── Target ──
  const targetNode = targetId ? nodes.find((n) => n.id === targetId) : null
  const targetAssetType = targetNode?.data.asset.type ?? null
  const targetSchema = targetAssetType
    ? objectTypeSchemas.find((s) => s.objectType === targetAssetType) ?? null
    : null
  const targetEligibleProps = useMemo(
    () =>
      mergeNumberMappingProperties(
        targetSchema,
        targetNode?.data.asset.metadata,
      ),
    [targetSchema, targetNode?.data.asset.metadata],
  )

  const linkSchema = linkTypeSchemas.find((s) => s.linkType === selectedLinkType) ?? null

  const bothSelected = sourceId != null && targetId != null
  const canProceedToType = bothSelected
  const canProceedToProps = canProceedToType && selectedLinkType !== ''

  const step = !bothSelected ? 1 : selectedLinkType === '' ? 2 : 3

  const assetLabel = (id: string | null) => {
    if (!id) return '(캔버스에서 클릭)'
    const node = nodes.find((n) => n.id === id)
    return node ? `${node.data.asset.type} (${id.slice(0, 8)}...)` : id
  }

  // ── Mapping helpers ──
  const getUnit = (props: typeof sourceEligibleProps, key: string) =>
    props.find((p) => p.key === key)?.unit ?? null

  const addMapping = () =>
    setMappings((prev) => [...prev, { fromProperty: '', toProperty: '', transformRule: 'value' }])

  const updateMapping = (idx: number, patch: Partial<MappingRow>) =>
    setMappings((prev) => prev.map((row, i) => (i === idx ? { ...row, ...patch } : row)))

  const removeMapping = (idx: number) =>
    setMappings((prev) => prev.filter((_, i) => i !== idx))

  // ── Submit ──
  const handleSubmit = async () => {
    if (!sourceId || !targetId || !selectedLinkType) return
    setSubmitError(null)
    setSubmitting(true)
    try {
      const validMappings: PropertyMapping[] = mappings
        .filter((m) => m.fromProperty && m.toProperty)
        .map((m) => ({
          fromProperty: m.fromProperty,
          toProperty: m.toProperty,
          transformRule: m.transformRule.trim() || 'value',
          fromUnit: getUnit(sourceEligibleProps, m.fromProperty),
          toUnit: getUnit(targetEligibleProps, m.toProperty),
        }))

      if (isEdit && editingRelationship) {
        const updated = await updateRelationship(editingRelationship.id, {
          relationshipType: selectedLinkType,
          properties: editingRelationship.properties ?? {},
          mappings: validMappings,
        })
        onSaved?.(updated)
      } else {
        const body: CreateRelationshipRequest = {
          fromAssetId: sourceId,
          toAssetId: targetId,
          relationshipType: selectedLinkType,
          properties: {},
          mappings: validMappings,
        }
        await createRelationship(body)
        onCreated?.()
      }
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : isEdit ? '저장 실패' : '생성 실패')
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!editingRelationship || !onDeleted) return
    if (!confirm('이 관계를 삭제할까요?')) return
    setSubmitError(null)
    setDeleting(true)
    try {
      await deleteRelationship(editingRelationship.id)
      onDeleted(editingRelationship.id)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : '삭제 실패')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <CanvasSidePanel className="assets-canvas-page__create-rel-panel">
      <div className="assets-canvas-page__side-panel-header">
        <h3>{isEdit ? '관계 편집' : '관계 설정'}</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>

      <div className="create-rel__steps">
        <div className={`create-rel__step-indicator ${step >= 1 ? 'active' : ''}`}>1</div>
        <div className={`create-rel__step-line ${step >= 2 ? 'active' : ''}`} />
        <div className={`create-rel__step-indicator ${step >= 2 ? 'active' : ''}`}>2</div>
        <div className={`create-rel__step-line ${step >= 3 ? 'active' : ''}`} />
        <div className={`create-rel__step-indicator ${step >= 3 ? 'active' : ''}`}>3</div>
      </div>

      <div className="create-rel__section">
        <span className="create-rel__section-title">Step 1 — 에셋 선택</span>
        <div className="create-rel__asset-slot">
          <label>Source (From)</label>
          <div className={`create-rel__asset-chip ${sourceId ? 'filled' : ''}`}>
            {assetLabel(sourceId)}
            {sourceId && !lockAssets && (
              <button type="button" onClick={() => onSetSource(null)} aria-label="source 해제">
                ×
              </button>
            )}
          </div>
        </div>
        <div className="create-rel__asset-slot">
          <label>Target (To)</label>
          <div className={`create-rel__asset-chip ${targetId ? 'filled' : ''}`}>
            {assetLabel(targetId)}
            {targetId && !lockAssets && (
              <button type="button" onClick={() => onSetTarget(null)} aria-label="target 해제">
                ×
              </button>
            )}
          </div>
        </div>
        {bothSelected && !lockAssets && (
          <button type="button" className="create-rel__swap-btn" onClick={onSwap}>
            ⇄ 방향 전환
          </button>
        )}
        {lockAssets && (
          <p className="create-rel__hint">이 관계의 From/To는 고정되어 있습니다.</p>
        )}
        {!bothSelected && !lockAssets && (
          <p className="create-rel__hint">캔버스에서 에셋을 클릭하여 선택하세요</p>
        )}
      </div>

      {canProceedToType && (
        <div className="create-rel__section">
          <span className="create-rel__section-title">Step 2 — 관계 타입</span>
          <select
            value={selectedLinkType}
            onChange={(e) => {
              const v = e.target.value
              setSelectedLinkType(v)
              const schema = linkTypeSchemas.find((s) => s.linkType === v)
              const defaults = schema?.defaultPropertyMappings
              if (defaults?.length && mappings.length === 0) {
                setMappings(toRows(defaults))
              }
            }}
            aria-label="관계 타입"
          >
            <option value="">선택하세요...</option>
            {linkTypeSchemas.map((s) => (
              <option key={s.linkType} value={s.linkType}>
                {s.displayName} ({s.linkType}) — {s.direction}
              </option>
            ))}
          </select>
          {linkSchema && (
            <p className="create-rel__hint">
              방향: {linkSchema.direction} / 지속성: {linkSchema.temporality}
              {linkSchema.allowedPropertyMappingPairs &&
                linkSchema.allowedPropertyMappingPairs.length > 0 && (
                  <>
                    {' '}
                    · 허용 매핑 쌍:{' '}
                    {linkSchema.allowedPropertyMappingPairs
                      .map(
                        (p) =>
                          `${p.fromPropertyKey}→${p.toPropertyKey}`,
                      )
                      .join(', ')}
                  </>
                )}
            </p>
          )}
        </div>
      )}

      {canProceedToProps && (
        <div className="create-rel__section">
          <span className="create-rel__section-title">Step 3 — 속성 매핑</span>

          <p className="create-rel__hint" style={{ marginBottom: '0.5rem' }}>
            Source → Target (Number). 예: <code>value</code>, <code>value * 0.2</code>,{' '}
            <code>min value 10</code>, <code>clamp value 0 100</code>, <code>abs value</code>. 단위
            불일치 시 kW↔W 등 백엔드 변환이 가능하면 런타임에 적용됩니다.
          </p>

          <div className="create-rel__mapping-list">
            {mappings.length === 0 && (
              <div className="create-rel__mapping-empty">
                매핑 없음 — 아래 버튼으로 추가하세요
              </div>
            )}
            {mappings.map((row, idx) => {
              const fromUnit = getUnit(sourceEligibleProps, row.fromProperty)
              const toUnit = getUnit(targetEligibleProps, row.toProperty)
              const unitMismatch = !!(fromUnit && toUnit && fromUnit !== toUnit)
              return (
                <div key={idx} className="create-rel__mapping-row">
                  {sourceEligibleProps.length > 0 ? (
                    <select
                      value={row.fromProperty}
                      onChange={(e) => updateMapping(idx, { fromProperty: e.target.value })}
                      aria-label="source 속성"
                    >
                      <option value="">source 속성...</option>
                      {sourceEligibleProps.map((p) => (
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

                  {targetEligibleProps.length > 0 ? (
                    <select
                      value={row.toProperty}
                      onChange={(e) => updateMapping(idx, { toProperty: e.target.value })}
                      aria-label="target 속성"
                    >
                      <option value="">target 속성...</option>
                      {targetEligibleProps.map((p) => (
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
                    {toUnit && <span className="create-rel__unit-badge">{toUnit}</span>}
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
      )}

      {submitError && (
        <p className="assets-canvas-page__error" style={{ marginTop: '0.5rem' }}>
          {submitError}
        </p>
      )}

      <div className="assets-canvas-page__panel-actions" style={{ marginTop: '1rem' }}>
        <button
          type="button"
          className="create-rel__submit-btn"
          disabled={submitting || !canProceedToProps}
          onClick={() => void handleSubmit()}
        >
          {submitting
            ? isEdit ? '저장 중…' : '생성 중…'
            : isEdit ? '저장' : '관계 생성'}
        </button>
        {isEdit && (
          <button
            type="button"
            onClick={() => void handleDelete()}
            disabled={deleting}
            className="assets-canvas-page__delete-btn"
          >
            {deleting ? '삭제 중…' : '삭제'}
          </button>
        )}
      </div>
    </CanvasSidePanel>
  )
}
