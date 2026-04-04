import { useEffect, useMemo, useState } from 'react'
import type { Node } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'
import {
  createRelationship,
  deleteRelationship,
  updateRelationship,
  type CreateRelationshipRequest,
  type RelationshipDto,
} from '@/entities/relationship'
import type { LinkTypeSchemaDto } from '@/entities/link-type-schema'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'
import { isEligibleProperty } from '@/shared/lib/canvasMetadata'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
import './CreateRelationshipOnPanel.css'

/** Matches canvas flow node `data` shape (see pages/canvas AssetNode). */
export type CreateRelationshipFlowNodeData = {
  asset: AssetDto
  liveStatus?: string
  liveProperties?: Record<string, unknown>
}

function parseTransfersFromProperties(
  properties: Record<string, unknown> | undefined,
): { keys: string[]; ratioFromTransfers: string } {
  const raw = properties?.transfers
  if (!Array.isArray(raw)) return { keys: [], ratioFromTransfers: '1' }
  const keys: string[] = []
  let ratioFromTransfers = '1'
  for (const item of raw) {
    if (
      item &&
      typeof item === 'object' &&
      'key' in item &&
      typeof (item as { key: unknown }).key === 'string'
    ) {
      keys.push((item as { key: string }).key)
      const r = (item as { ratio?: unknown }).ratio
      if (typeof r === 'number' && Number.isFinite(r)) {
        ratioFromTransfers = String(r)
      }
    }
  }
  return { keys, ratioFromTransfers }
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
  const [transferKeys, setTransferKeys] = useState<string[]>([])
  const [ratio, setRatio] = useState('1')
  const [submitting, setSubmitting] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (!editingRelationship) {
      setSelectedLinkType('')
      setTransferKeys([])
      setRatio('1')
      setSubmitError(null)
      return
    }
    setSelectedLinkType(editingRelationship.relationshipType)
    const { keys, ratioFromTransfers } = parseTransfersFromProperties(
      editingRelationship.properties,
    )
    setTransferKeys(keys)
    const linkRatio = editingRelationship.properties?.ratio
    if (typeof linkRatio === 'number' && Number.isFinite(linkRatio)) {
      setRatio(String(linkRatio))
    } else {
      setRatio(ratioFromTransfers)
    }
    setSubmitError(null)
  }, [editingRelationship])

  const sourceNode = sourceId ? nodes.find((n) => n.id === sourceId) : null
  const sourceAssetType = sourceNode?.data.asset.type ?? null
  const linkSchema = linkTypeSchemas.find((s) => s.linkType === selectedLinkType) ?? null

  const sourceSchema = sourceAssetType
    ? objectTypeSchemas.find((s) => s.objectType === sourceAssetType) ?? null
    : null

  const eligibleProps = useMemo(() => {
    if (!sourceSchema) return []
    return (sourceSchema.resolvedProperties ?? sourceSchema.ownProperties).filter(isEligibleProperty)
  }, [sourceSchema])

  const sourceAsset = sourceNode?.data.asset ?? null

  const metadataEligibleProps = useMemo(() => {
    if (!sourceAsset) return []
    const schemaKeys = new Set(eligibleProps.map((p) => p.key))
    return Object.keys(sourceAsset.metadata ?? {})
      .filter((key) => !schemaKeys.has(key))
      .map((key) => ({ key }))
  }, [sourceAsset, eligibleProps])

  const bothSelected = sourceId != null && targetId != null
  const canProceedToType = bothSelected
  const canProceedToProps = canProceedToType && selectedLinkType !== ''
  const hasTransfersProperty = linkSchema?.properties.some((p) => p.key === 'transfers') ?? false
  const hasRatioProperty = linkSchema?.properties.some((p) => p.key === 'ratio') ?? false

  const step = !bothSelected ? 1 : selectedLinkType === '' ? 2 : 3

  const assetLabel = (id: string | null) => {
    if (!id) return '(캔버스에서 클릭)'
    const node = nodes.find((n) => n.id === id)
    return node ? `${node.data.asset.type} (${id.slice(0, 8)}...)` : id
  }

  const buildPropertiesPayload = (): Record<string, unknown> => {
    const properties: Record<string, unknown> = {}
    if (isEdit && editingRelationship?.properties) {
      for (const [k, v] of Object.entries(editingRelationship.properties)) {
        if (k === 'transfers' || k === 'ratio') continue
        properties[k] = v
      }
    }
    if (hasTransfersProperty) {
      const r = parseFloat(ratio) || 1
      if (transferKeys.length > 0) {
        properties.transfers = transferKeys.map((key) => ({ key, ratio: r }))
      } else if (isEdit) {
        properties.transfers = []
      }
    }
    if (hasRatioProperty) {
      properties.ratio = parseFloat(ratio) || 1
    }
    return properties
  }

  const handleSubmit = async () => {
    if (!sourceId || !targetId || !selectedLinkType) return
    setSubmitError(null)
    setSubmitting(true)
    try {
      const properties = buildPropertiesPayload()
      if (isEdit && editingRelationship) {
        const updated = await updateRelationship(editingRelationship.id, {
          relationshipType: selectedLinkType,
          properties,
        })
        onSaved?.(updated)
      } else {
        const body: CreateRelationshipRequest = {
          fromAssetId: sourceId,
          toAssetId: targetId,
          relationshipType: selectedLinkType,
          properties,
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
        <h3>관계 설정</h3>
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

          {hasTransfersProperty && (eligibleProps.length > 0 || metadataEligibleProps.length > 0) && (
            <div className="create-rel__transfers">
              <span className="create-rel__sub-label">전파할 속성 (transfers)</span>
              <p className="create-rel__hint">
                Source 에셋의 속성 중 관계를 통해 전파할 항목을 선택하세요
              </p>
              {eligibleProps.length > 0 && (
                <>
                  <span className="create-rel__transfer-group">[스키마 속성]</span>
                  {eligibleProps.map((p) => (
                    <label key={p.key} className="create-rel__checkbox-label">
                      <input
                        type="checkbox"
                        checked={transferKeys.includes(p.key)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            setTransferKeys((k) => [...k, p.key])
                          } else {
                            setTransferKeys((k) => k.filter((x) => x !== p.key))
                          }
                        }}
                      />
                      <span>{p.key}</span>
                      <span className="create-rel__prop-meta">
                        {p.dataType} / {p.simulationBehavior}
                        {p.baseValue != null ? ` (base: ${p.baseValue})` : ''}
                      </span>
                    </label>
                  ))}
                </>
              )}
              {metadataEligibleProps.length > 0 && (
                <>
                  <span className="create-rel__transfer-group">[에셋 메타데이터]</span>
                  {metadataEligibleProps.map(({ key }) => {
                    const raw = sourceAsset?.metadata?.[key]
                    const display =
                      typeof raw === 'string' ? raw : raw != null ? JSON.stringify(raw) : ''
                    return (
                      <label key={key} className="create-rel__checkbox-label">
                        <input
                          type="checkbox"
                          checked={transferKeys.includes(key)}
                          onChange={(e) => {
                            if (e.target.checked) {
                              setTransferKeys((k) => [...k, key])
                            } else {
                              setTransferKeys((k) => k.filter((x) => x !== key))
                            }
                          }}
                        />
                        <span>{key}</span>
                        <span className="create-rel__prop-meta">현재 값: {display}</span>
                      </label>
                    )
                  })}
                </>
              )}
            </div>
          )}

          {hasTransfersProperty &&
            eligibleProps.length === 0 &&
            metadataEligibleProps.length === 0 &&
            sourceSchema && (
            <p className="create-rel__hint">
              Source 에셋({sourceAssetType})에 전파 가능한 속성이 없습니다
            </p>
          )}

          {hasTransfersProperty &&
            eligibleProps.length === 0 &&
            metadataEligibleProps.length === 0 &&
            !sourceSchema && (
            <p className="create-rel__hint">
              Source 에셋의 ObjectTypeSchema가 없어 속성을 자동 제공할 수 없습니다
            </p>
          )}

          {hasRatioProperty && (
            <label className="create-rel__ratio-label">
              Ratio
              <input
                type="number"
                step="0.01"
                min="0"
                max="1"
                value={ratio}
                onChange={(e) => setRatio(e.target.value)}
              />
            </label>
          )}

          {linkSchema && linkSchema.properties.length === 0 && (
            <p className="create-rel__hint">이 관계 타입에는 추가 속성이 없습니다</p>
          )}

          {submitError && <p className="assets-canvas-page__error">{submitError}</p>}

          <button
            type="button"
            className="create-rel__submit-btn"
            disabled={submitting}
            onClick={() => void handleSubmit()}
          >
            {submitting
              ? isEdit
                ? '저장 중...'
                : '생성 중...'
              : isEdit
                ? '저장'
                : '관계 생성'}
          </button>

          {isEdit && onDeleted && (
            <button
              type="button"
              className="assets-canvas-page__delete-btn create-rel__delete-btn"
              disabled={deleting}
              onClick={() => void handleDelete()}
            >
              {deleting ? '삭제 중…' : '삭제'}
            </button>
          )}
        </div>
      )}
    </CanvasSidePanel>
  )
}
