import { useCallback, useEffect, useState } from 'react'
import {
  ReactFlow,
  Controls,
  Background,
  BaseEdge,
  getBezierPath,
  useNodesState,
  useEdgesState,
  type Node,
  type Edge,
  type EdgeProps,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { getAssets, createAsset, updateAsset, type AssetDto } from '@/entities/asset'
import {
  getRelationships,
  createRelationship,
  updateRelationship,
  deleteRelationship,
  RELATIONSHIP_TYPE_OPTIONS,
  type CreateRelationshipRequest,
  type RelationshipDto,
} from '@/entities/relationship'
import type { AssetNodeData } from './AssetNode'
import { AssetNode } from './AssetNode'
import './AssetsCanvasPage.css'

const NODE_TYPE = 'asset'
const EDGE_TYPE = 'relationshipEdge'
const GRID_X = 220
const GRID_Y = 140

type RelationshipEdgeData = { relationship: RelationshipDto }
type RelationshipEdge = Edge<RelationshipEdgeData>

function RelationshipEdgeComponent(props: EdgeProps<RelationshipEdge>) {
  const { id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition } = props
  const [path] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  })
  return (
    <g data-testid="relationship-edge">
      <BaseEdge id={id} path={path} />
    </g>
  )
}

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

export function AssetsCanvasPage() {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node<AssetNodeData>>([])
  const [edges, setEdges, onEdgesChange] = useEdgesState<RelationshipEdge>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null)
  const [selectedEdgeId, setSelectedEdgeId] = useState<string | null>(null)
  const [relDialogOpen, setRelDialogOpen] = useState(false)
  const [addFormOpen, setAddFormOpen] = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [assets, relationships] = await Promise.all([getAssets(), getRelationships()])
      const assetIds = new Set(assets.map((a) => a.id))
      const flowNodes: Node<AssetNodeData>[] = assets.map((asset, i) => ({
        id: asset.id,
        type: NODE_TYPE,
        position: { x: i * GRID_X, y: i * GRID_Y },
        data: { asset },
      }))
      const flowEdges: RelationshipEdge[] = relationships
        .filter((r) => assetIds.has(r.fromAssetId) && assetIds.has(r.toAssetId))
        .map((r) => ({
          id: r.id,
          type: EDGE_TYPE,
          source: r.fromAssetId,
          target: r.toAssetId,
          data: { relationship: r },
        }))
      setNodes(flowNodes)
      setEdges(flowEdges)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load')
    } finally {
      setLoading(false)
    }
  }, [setNodes, setEdges])

  useEffect(() => {
    loadData()
  }, [loadData])

  const onNodeClick = useCallback((_: React.MouseEvent, node: Node<AssetNodeData>) => {
    setSelectedEdgeId(null)
    setSelectedNodeId(node.id)
  }, [])

  const onEdgeClick = useCallback((_: React.MouseEvent, edge: RelationshipEdge) => {
    setSelectedNodeId(null)
    setSelectedEdgeId(edge.id)
  }, [])

  const onPaneClick = useCallback(() => {
    setSelectedNodeId(null)
    setSelectedEdgeId(null)
  }, [])

  const selectedNode = selectedNodeId ? nodes.find((n) => n.id === selectedNodeId) : null
  const selectedEdge = selectedEdgeId
    ? edges.find((e) => e.id === selectedEdgeId)
    : null
  const selectedRelationship = selectedEdge?.data?.relationship ?? null
  const selectedNodesForRel = nodes.filter((n) => n.selected)
  const canCreateRel =
    selectedNodesForRel.length === 2 &&
    !edges.some(
      (e) =>
        e.source === selectedNodesForRel[0].id && e.target === selectedNodesForRel[1].id
    )

  if (loading) return <div className="assets-canvas-page">로딩 중...</div>
  if (error) return <div className="assets-canvas-page">에러: {error}</div>

  return (
    <div className="assets-canvas-page">
      <div className="assets-canvas-page__toolbar">
        <button type="button" onClick={() => setAddFormOpen(true)}>
          에셋 추가
        </button>
        <button
          type="button"
          disabled={!canCreateRel}
          onClick={() => setRelDialogOpen(true)}
          title={selectedNodesForRel.length === 2 ? '두 노드 사이에 관계 생성' : '노드 2개를 선택하세요'}
        >
          관계 만들기
        </button>
      </div>

      <div className="assets-canvas-page__main">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onNodeClick={onNodeClick}
          onEdgeClick={onEdgeClick}
          onPaneClick={onPaneClick}
          nodeTypes={{ [NODE_TYPE]: AssetNode }}
          edgeTypes={{ [EDGE_TYPE]: RelationshipEdgeComponent }}
          fitView
          fitViewOptions={{ padding: 0.2 }}
        >
          <Controls />
          <Background />
        </ReactFlow>

        {selectedNode && (
          <SidePanel
            node={selectedNode}
            onClose={() => setSelectedNodeId(null)}
            onSave={async (type, metadata) => {
              const asset = (selectedNode.data as AssetNodeData).asset
              const connections = edges
                .filter((e) => e.source === selectedNode.id)
                .map((e) => e.target as string)
              await updateAsset(selectedNode.id, {
                type: type || asset.type,
                connections,
                metadata: Object.keys(metadata).length ? metadata : undefined,
              })
              setNodes((nds) =>
                nds.map((n) =>
                  n.id === selectedNode.id
                    ? { ...n, data: { asset: { ...asset, type: type || asset.type, metadata } } }
                    : n
                )
              )
              setSelectedNodeId(null)
            }}
          />
        )}
        {selectedRelationship && selectedEdge && (
          <RelationshipEditPanel
            relationship={selectedRelationship}
            onClose={() => setSelectedEdgeId(null)}
            onSaved={(updated) => {
              setEdges((eds) =>
                eds.map((e) =>
                  e.id === updated.id
                    ? { ...e, data: { relationship: updated } }
                    : e
                )
              )
              setSelectedEdgeId(null)
            }}
            onDeleted={(id) => {
              setEdges((eds) => eds.filter((e) => e.id !== id))
              setSelectedEdgeId(null)
            }}
          />
        )}
      </div>

      {addFormOpen && (
        <AddAssetModal
          onClose={() => setAddFormOpen(false)}
          onCreated={(asset) => {
            const last = nodes[nodes.length - 1]
            const pos = last
              ? { x: last.position.x + GRID_X, y: last.position.y }
              : { x: 0, y: 0 }
            setNodes((nds) => [
              ...nds,
              {
                id: asset.id,
                type: NODE_TYPE,
                position: pos,
                data: { asset },
              },
            ])
            setAddFormOpen(false)
          }}
        />
      )}

      {relDialogOpen && selectedNodesForRel.length === 2 && (
        <RelationshipDialog
          fromAssetId={selectedNodesForRel[0].id}
          toAssetId={selectedNodesForRel[1].id}
          onClose={() => setRelDialogOpen(false)}
          onCreated={(rel) => {
            setEdges((eds) => [
              ...eds,
              {
                id: rel.id,
                type: EDGE_TYPE,
                source: rel.fromAssetId,
                target: rel.toAssetId,
                data: { relationship: rel },
              },
            ])
            setRelDialogOpen(false)
          }}
        />
      )}
    </div>
  )
}

function SidePanel({
  node,
  onClose,
  onSave,
}: {
  node: Node<AssetNodeData>
  onClose: () => void
  onSave: (type: string, metadata: Record<string, unknown>) => Promise<void>
}) {
  const asset = node.data.asset
  const [type, setType] = useState(asset.type)
  const [metaRows, setMetaRows] = useState(() => recordToMetadataRows(asset.metadata ?? {}))
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [showSystemInfo, setShowSystemInfo] = useState(false)

  const addRow = () => setMetaRows((r) => [...r, { key: '', value: '' }])
  const removeRow = (i: number) => setMetaRows((r) => r.filter((_, idx) => idx !== i))
  const setRow = (i: number, field: 'key' | 'value', value: string) =>
    setMetaRows((r) => r.map((row, idx) => (idx === i ? { ...row, [field]: value } : row)))

  const copyId = () => {
    void navigator.clipboard.writeText(asset.id)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      await onSave(type.trim(), formMetadataToRecord(metaRows))
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="assets-canvas-page__side-panel">
      <div className="assets-canvas-page__side-panel-header">
        <h3>에셋 편집</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>
      <div className="assets-canvas-page__system-actions">
        <button type="button" onClick={copyId} className="assets-canvas-page__copy-id-btn">
          ID 복사
        </button>
        <button
          type="button"
          onClick={() => setShowSystemInfo((v) => !v)}
          className="assets-canvas-page__toggle-system"
          aria-expanded={showSystemInfo}
        >
          {showSystemInfo ? '시스템 정보 접기' : '시스템 정보'}
        </button>
      </div>
      {showSystemInfo && (
        <div className="assets-canvas-page__system-fields" aria-label="시스템 정보">
          <dl>
            <dt>id</dt>
            <dd>{asset.id}</dd>
            <dt>createdAt</dt>
            <dd>{asset.createdAt ? new Date(asset.createdAt).toLocaleString() : '-'}</dd>
            <dt>updatedAt</dt>
            <dd>{asset.updatedAt ? new Date(asset.updatedAt).toLocaleString() : '-'}</dd>
          </dl>
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <label>
          Type
          <input
            type="text"
            value={type}
            onChange={(e) => setType(e.target.value)}
            required
          />
        </label>
        <div className="assets-canvas-page__meta-section">
          <span>Metadata</span>
          {metaRows.map((row, i) => (
            <div key={i} className="assets-canvas-page__meta-row">
              <input
                placeholder="key"
                value={row.key}
                onChange={(e) => setRow(i, 'key', e.target.value)}
              />
              <input
                placeholder="value"
                value={row.value}
                onChange={(e) => setRow(i, 'value', e.target.value)}
              />
              <button type="button" onClick={() => removeRow(i)}>
                삭제
              </button>
            </div>
          ))}
          <button type="button" onClick={addRow}>
            항목 추가
          </button>
        </div>
        {saveError && <p className="assets-canvas-page__error">{saveError}</p>}
        <button type="submit" disabled={saving}>
          {saving ? '저장 중…' : '저장'}
        </button>
      </form>
    </div>
  )
}

function AddAssetModal({
  onClose,
  onCreated,
}: {
  onClose: () => void
  onCreated: (asset: AssetDto) => void
}) {
  const [type, setType] = useState('')
  const [metaRows, setMetaRows] = useState<Array<{ key: string; value: string }>>([])
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const addRow = () => setMetaRows((r) => [...r, { key: '', value: '' }])
  const removeRow = (i: number) => setMetaRows((r) => r.filter((_, idx) => idx !== i))
  const setRow = (i: number, field: 'key' | 'value', value: string) =>
    setMetaRows((r) => r.map((row, idx) => (idx === i ? { ...row, [field]: value } : row)))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const t = type.trim()
    if (!t) {
      setCreateError('Type을 입력하세요')
      return
    }
    setSubmitting(true)
    try {
      const asset = await createAsset({
        type: t,
        connections: [],
        metadata: formMetadataToRecord(metaRows),
      })
      onCreated(asset)
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
            Type <span className="required">*</span>
            <input
              type="text"
              value={type}
              onChange={(e) => setType(e.target.value)}
              placeholder="예: freezer"
            />
          </label>
          <div className="assets-canvas-page__meta-section">
            <span>Metadata</span>
            {metaRows.map((row, i) => (
              <div key={i} className="assets-canvas-page__meta-row">
                <input
                  placeholder="key"
                  value={row.key}
                  onChange={(e) => setRow(i, 'key', e.target.value)}
                />
                <input
                  placeholder="value"
                  value={row.value}
                  onChange={(e) => setRow(i, 'value', e.target.value)}
                />
                <button type="button" onClick={() => removeRow(i)}>
                  삭제
                </button>
              </div>
            ))}
            <button type="button" onClick={addRow}>
              항목 추가
            </button>
          </div>
          {createError && <p className="assets-canvas-page__error">{createError}</p>}
          <button type="submit" disabled={submitting}>
            {submitting ? '생성 중…' : '생성'}
          </button>
        </form>
      </div>
    </div>
  )
}

export function RelationshipEditPanel({
  relationship,
  onClose,
  onSaved,
  onDeleted,
}: {
  relationship: RelationshipDto
  onClose: () => void
  onSaved: (updated: RelationshipDto) => void
  onDeleted: (id: string) => void
}) {
  const [relationshipType, setRelationshipType] = useState(relationship.relationshipType)
  const [propertiesJson, setPropertiesJson] = useState(() =>
    JSON.stringify(relationship.properties ?? {}, null, 2)
  )
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

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

  const handleSave = async (e: React.FormEvent) => {
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
    <div className="assets-canvas-page__side-panel">
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
          <select
            value={relationshipType}
            onChange={(e) => setRelationshipType(e.target.value)}
          >
            {RELATIONSHIP_TYPE_OPTIONS.map((opt) => (
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
            onClick={handleDelete}
            disabled={deleting}
            className="assets-canvas-page__delete-btn"
          >
            {deleting ? '삭제 중…' : '삭제'}
          </button>
        </div>
      </form>
    </div>
  )
}

function RelationshipDialog({
  fromAssetId,
  toAssetId,
  onClose,
  onCreated,
}: {
  fromAssetId: string
  toAssetId: string
  onClose: () => void
  onCreated: (rel: { id: string; fromAssetId: string; toAssetId: string }) => void
}) {
  const [relationshipType, setRelationshipType] = useState('feeds_into')
  const [propertiesJson, setPropertiesJson] = useState('{}')
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const t = relationshipType.trim()
    if (!t) {
      setCreateError('관계 타입을 입력하세요')
      return
    }
    setSubmitting(true)
    try {
      const body: CreateRelationshipRequest = {
        fromAssetId,
        toAssetId,
        relationshipType: t,
        properties: parseProps(),
      }
      const rel = await createRelationship(body)
      onCreated(rel)
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
          <h3>관계 만들기</h3>
          <button type="button" onClick={onClose} aria-label="닫기">
            ×
          </button>
        </div>
        <p className="assets-canvas-page__rel-hint">
          From: {fromAssetId} → To: {toAssetId}
        </p>
        <form onSubmit={handleSubmit}>
          <label>
            관계 타입 <span className="required">*</span>
            <select
              value={relationshipType}
              onChange={(e) => setRelationshipType(e.target.value)}
            >
              {RELATIONSHIP_TYPE_OPTIONS.map((opt) => (
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
          {createError && <p className="assets-canvas-page__error">{createError}</p>}
          <button type="submit" disabled={submitting}>
            {submitting ? '생성 중…' : '생성'}
          </button>
        </form>
      </div>
    </div>
  )
}
