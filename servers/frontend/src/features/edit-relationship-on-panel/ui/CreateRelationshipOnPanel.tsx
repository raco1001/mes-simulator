import { useMemo, useState } from 'react'
import type { Node } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'
import { createRelationship, type CreateRelationshipRequest, type PropertyMapping } from '@/entities/relationship'
import type { LinkTypeSchemaDto } from '@/entities/link-type-schema'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { isEligibleProperty } from '@/shared/lib/canvasMetadata'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
import './CreateRelationshipOnPanel.css'

interface MappingRow {
  fromProperty: string
  toProperty: string
  transformRule: string
}

/** Matches canvas flow node `data` shape (see pages/canvas AssetNode). */
export type CreateRelationshipFlowNodeData = {
  asset: AssetDto
  liveStatus?: string
  liveProperties?: Record<string, unknown>
}

export function CreateRelationshipOnPanel({
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
}: {
  sourceId: string | null
  targetId: string | null
  nodes: Node<CreateRelationshipFlowNodeData>[]
  linkTypeSchemas: LinkTypeSchemaDto[]
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onSetSource: (id: string | null) => void
  onSetTarget: (id: string | null) => void
  onSwap: () => void
  onClose: () => void
  onCreated: () => void
}) {
  const [selectedLinkType, setSelectedLinkType] = useState('')
  const [mappings, setMappings] = useState<MappingRow[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  // ── Source ──
  const sourceNode = sourceId ? nodes.find((n) => n.id === sourceId) : null
  const sourceAssetType = sourceNode?.data.asset.type ?? null
  const sourceSchema = sourceAssetType
    ? objectTypeSchemas.find((s) => s.objectType === sourceAssetType) ?? null
    : null
  const sourceEligibleProps = useMemo(() => {
    if (!sourceSchema) return []
    return (sourceSchema.resolvedProperties ?? sourceSchema.ownProperties).filter(isEligibleProperty)
  }, [sourceSchema])

  // ── Target ──
  const targetNode = targetId ? nodes.find((n) => n.id === targetId) : null
  const targetAssetType = targetNode?.data.asset.type ?? null
  const targetSchema = targetAssetType
    ? objectTypeSchemas.find((s) => s.objectType === targetAssetType) ?? null
    : null
  const targetEligibleProps = useMemo(() => {
    if (!targetSchema) return []
    return (targetSchema.resolvedProperties ?? targetSchema.ownProperties).filter(isEligibleProperty)
  }, [targetSchema])

  const linkSchema = linkTypeSchemas.find((s) => s.linkType === selectedLinkType) ?? null
  const hasTransfersProperty = linkSchema?.properties.some((p) => p.key === 'transfers') ?? false

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
    setCreateError(null)
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

      const body: CreateRelationshipRequest = {
        fromAssetId: sourceId,
        toAssetId: targetId,
        relationshipType: selectedLinkType,
        properties: {},
        mappings: validMappings,
      }
      await createRelationship(body)
      onCreated()
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : '생성 실패')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <CanvasSidePanel className="assets-canvas-page__create-rel-panel">
      <div className="assets-canvas-page__side-panel-header">
        <h3>관계 만들기</h3>
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
            {sourceId && (
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
            {targetId && (
              <button type="button" onClick={() => onSetTarget(null)} aria-label="target 해제">
                ×
              </button>
            )}
          </div>
        </div>
        {bothSelected && (
          <button type="button" className="create-rel__swap-btn" onClick={onSwap}>
            ⇄ 방향 전환
          </button>
        )}
        {!bothSelected && (
          <p className="create-rel__hint">캔버스에서 에셋을 클릭하여 선택하세요</p>
        )}
      </div>

      {canProceedToType && (
        <div className="create-rel__section">
          <span className="create-rel__section-title">Step 2 — 관계 타입</span>
          <select
            value={selectedLinkType}
            onChange={(e) => {
              setSelectedLinkType(e.target.value)
              setTransferKeys([])
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
            </p>
          )}
        </div>
      )}

      {canProceedToProps && (
        <div className="create-rel__section">
          <span className="create-rel__section-title">Step 3 — 속성 설정</span>

          {hasTransfersProperty ? (
            <div className="create-rel__transfers">
              <span className="create-rel__sub-label">전파 속성 (transfers)</span>
              <p className="create-rel__hint" style={{ marginBottom: '0.5rem' }}>
                Source → Target 속성을 연결하고 연산 규칙을 입력하세요.{' '}
                예: <code>value</code>, <code>value * 0.2</code>, <code>value / 3</code>
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

                      <span className="create-rel__mapping-arrow">→</span>

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
                disabled={sourceEligibleProps.length === 0 || targetEligibleProps.length === 0}
              >
                + 매핑 추가
              </button>

              {sourceEligibleProps.length === 0 && sourceSchema && (
                <p className="create-rel__hint" style={{ marginTop: '0.4rem' }}>
                  ⚠ Source({sourceAssetType})에 전파 가능한 속성이 없습니다
                </p>
              )}
              {targetEligibleProps.length === 0 && targetSchema && (
                <p className="create-rel__hint" style={{ marginTop: '0.4rem' }}>
                  ⚠ Target({targetAssetType})에 전파 가능한 속성이 없습니다
                </p>
              )}
            </div>
          ) : (
            <p className="create-rel__hint">
              이 관계 타입({selectedLinkType})은 속성 전파를 지원하지 않습니다
            </p>
          )}

          {createError && <p className="assets-canvas-page__error">{createError}</p>}

          <button
            type="button"
            className="create-rel__submit-btn"
            disabled={submitting}
            onClick={handleSubmit}
          >
            {submitting ? '생성 중...' : '저장'}
          </button>
        </div>
      )}
    </CanvasSidePanel>
  )
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          