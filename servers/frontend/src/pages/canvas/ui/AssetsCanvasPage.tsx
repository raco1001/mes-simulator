import { useCallback, useEffect, useMemo, useState } from 'react'
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
import {
  getAssets,
  createAsset,
  updateAsset,
  deleteAsset,
  type AssetDto,
} from '@/entities/asset'
import {
  getObjectTypeSchemas,
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
import {
  getLinkTypeSchemas,
  type LinkTypeSchemaDto,
} from '@/entities/link-type-schema'
import {
  getRelationships,
  createRelationship,
  updateRelationship,
  deleteRelationship,
  type CreateRelationshipRequest,
  type RelationshipDto,
} from '@/entities/relationship'
import {
  runSimulation,
  startContinuousRun,
  stopRun,
  getRunEvents,
  subscribeSimulationEvents,
  type EventDto,
} from '@/entities/simulation'
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
  const {
    id,
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  } = props
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

function isEligibleProperty(p: PropertyDefinition): boolean {
  return (
    p.dataType === 'Number' &&
    (['Settable', 'Rate', 'Accumulator'] as SimulationBehavior[]).includes(p.simulationBehavior) &&
    p.mutability === 'Mutable'
  )
}

/** ObjectType 변경 시 스키마 기본값 주입 + 스키마에 없던 키는 유지 */
function buildMetadataFromTypeSelection(
  objectType: string,
  schemas: ObjectTypeSchemaDto[],
  previousMeta: Record<string, unknown>,
): Record<string, unknown> {
  const schema = schemas.find((s) => s.objectType === objectType)
  const props = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeys = new Set(props.map((p) => p.key))
  const injected: Record<string, unknown> = {}
  for (const p of props) {
    injected[p.key] = p.baseValue ?? ''
  }
  const preserved: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(previousMeta)) {
    if (!schemaKeys.has(k)) preserved[k] = v
  }
  return { ...injected, ...preserved }
}

/** 패널 오픈 시 에셋 메타 + 스키마 기본값 병합 */
function mergeAssetMetadataWithSchema(
  objectType: string,
  schemas: ObjectTypeSchemaDto[],
  assetMeta: Record<string, unknown>,
): Record<string, unknown> {
  const schema = schemas.find((s) => s.objectType === objectType)
  const props = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const out: Record<string, unknown> = { ...assetMeta }
  for (const p of props) {
    if (out[p.key] === undefined) out[p.key] = p.baseValue ?? ''
  }
  return out
}

export function AssetsCanvasPage() {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node<AssetNodeData>>([])
  const [edges, setEdges, onEdgesChange] = useEdgesState<RelationshipEdge>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [objectTypeSchemas, setObjectTypeSchemas] = useState<ObjectTypeSchemaDto[]>([])
  const [linkTypeSchemas, setLinkTypeSchemas] = useState<LinkTypeSchemaDto[]>([])
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null)
  const [selectedEdgeId, setSelectedEdgeId] = useState<string | null>(null)
  const [addFormOpen, setAddFormOpen] = useState(false)

  const [relMode, setRelMode] = useState(false)
  const [relSourceId, setRelSourceId] = useState<string | null>(null)
  const [relTargetId, setRelTargetId] = useState<string | null>(null)
  const [simPanelOpen, setSimPanelOpen] = useState(false)
  const [objectTypePanelOpen, setObjectTypePanelOpen] = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [assets, relationships, schemas, linkSchemas] = await Promise.all([
        getAssets(),
        getRelationships(),
        getObjectTypeSchemas().catch(() => []),
        getLinkTypeSchemas().catch(() => []),
      ])
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
      setObjectTypeSchemas(schemas)
      setLinkTypeSchemas(linkSchemas)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load')
    } finally {
      setLoading(false)
    }
  }, [setNodes, setEdges])

  useEffect(() => {
    loadData()
  }, [loadData])

  const enterRelMode = useCallback(() => {
    setRelMode(true)
    setRelSourceId(null)
    setRelTargetId(null)
    setSelectedNodeId(null)
    setSelectedEdgeId(null)
    setSimPanelOpen(false)
    setObjectTypePanelOpen(false)
  }, [])

  const exitRelMode = useCallback(() => {
    setRelMode(false)
    setRelSourceId(null)
    setRelTargetId(null)
  }, [])

  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: Node<AssetNodeData>) => {
      if (relMode) {
        if (!relSourceId) {
          setRelSourceId(node.id)
        } else if (!relTargetId && node.id !== relSourceId) {
          setRelTargetId(node.id)
        } else if (node.id !== relSourceId && node.id !== relTargetId) {
          setRelTargetId(node.id)
        }
        return
      }
      setSelectedEdgeId(null)
      setSelectedNodeId(node.id)
    },
    [relMode, relSourceId, relTargetId],
  )

  const onEdgeClick = useCallback(
    (_: React.MouseEvent, edge: RelationshipEdge) => {
      if (relMode) return
      setSelectedNodeId(null)
      setSelectedEdgeId(edge.id)
    },
    [relMode],
  )

  const onPaneClick = useCallback(() => {
    if (relMode) return
    setSelectedNodeId(null)
    setSelectedEdgeId(null)
  }, [relMode])

  const highlightedNodes = useMemo(() => {
    return nodes.map((n) => {
      let className = ''
      if (relMode && n.id === relSourceId) className = 'rel-source-highlight'
      else if (relMode && n.id === relTargetId) className = 'rel-target-highlight'
      return className !== (n.className ?? '') ? { ...n, className } : n
    })
  }, [nodes, relMode, relSourceId, relTargetId])

  const selectedNode = selectedNodeId
    ? highlightedNodes.find((n) => n.id === selectedNodeId)
    : null
  const selectedEdge = selectedEdgeId
    ? edges.find((e) => e.id === selectedEdgeId)
    : null
  const selectedRelationship = selectedEdge?.data?.relationship ?? null

  const isEmpty = !loading && !error && nodes.length === 0

  const toggleSimPanel = useCallback(() => {
    setSimPanelOpen((v) => !v)
    if (!simPanelOpen) {
      exitRelMode()
      setSelectedNodeId(null)
      setSelectedEdgeId(null)
      setObjectTypePanelOpen(false)
    }
  }, [simPanelOpen, exitRelMode])

  const toggleObjectTypePanel = useCallback(() => {
    setObjectTypePanelOpen((open) => {
      if (!open) {
        setRelMode(false)
        setRelSourceId(null)
        setRelTargetId(null)
        setSimPanelOpen(false)
        setSelectedNodeId(null)
        setSelectedEdgeId(null)
        return true
      }
      return false
    })
  }, [])

  const refreshObjectTypeSchemas = useCallback(async () => {
    try {
      const schemas = await getObjectTypeSchemas()
      setObjectTypeSchemas(schemas)
    } catch {
      setObjectTypeSchemas([])
    }
  }, [])

  const showCreatePanel = relMode && !objectTypePanelOpen
  const showAssetPanel =
    !relMode && !simPanelOpen && !objectTypePanelOpen && selectedNode != null
  const showRelEditPanel =
    !relMode &&
    !simPanelOpen &&
    !objectTypePanelOpen &&
    selectedRelationship != null &&
    selectedEdge != null
  const showSimPanel = simPanelOpen && !relMode && !objectTypePanelOpen
  const showObjectTypePanel = objectTypePanelOpen && !relMode

  const assets: AssetDto[] = useMemo(() => nodes.map((n) => n.data.asset), [nodes])

  if (loading) return <div className="assets-canvas-page">Loading...</div>
  if (error) return <div className="assets-canvas-page">Error: {error}</div>

  return (
    <div className={`assets-canvas-page ${relMode ? 'assets-canvas-page--rel-mode' : ''}`}>
      <div className="assets-canvas-page__toolbar">
        <button type="button" onClick={() => setAddFormOpen(true)}>
          에셋 추가
        </button>
        {relMode ? (
          <button
            type="button"
            className="assets-canvas-page__toolbar-cancel"
            onClick={exitRelMode}
          >
            관계 만들기 취소
          </button>
        ) : (
          <button
            type="button"
            onClick={enterRelMode}
            disabled={nodes.length < 2 || objectTypePanelOpen}
            title="캔버스에서 에셋을 클릭하여 관계를 만들 수 있습니다"
          >
            관계 만들기
          </button>
        )}
        <button
          type="button"
          className={objectTypePanelOpen ? 'assets-canvas-page__toolbar-active' : ''}
          onClick={toggleObjectTypePanel}
          disabled={relMode}
        >
          {objectTypePanelOpen ? 'ObjectType 닫기' : 'ObjectType 관리'}
        </button>
        <button
          type="button"
          className={simPanelOpen ? 'assets-canvas-page__toolbar-active' : ''}
          onClick={toggleSimPanel}
          disabled={relMode || objectTypePanelOpen}
        >
          {simPanelOpen ? '시뮬레이션 닫기' : '시뮬레이션'}
        </button>
        {relMode && (
          <span className="assets-canvas-page__rel-indicator">
            관계 편집 모드 — 캔버스에서 에셋을 클릭하세요
          </span>
        )}
      </div>

      <div className="assets-canvas-page__main">
        {isEmpty && (
          <div className="assets-canvas-page__onboarding">
            <h2>Factory MES에 오신 것을 환영합니다</h2>
            <p>공장 에셋을 추가하고 관계를 연결하여 디지털 트윈을 구성하세요.</p>
            <p>에셋을 추가한 뒤 &quot;관계 만들기&quot; 버튼을 누르세요.</p>
            <button type="button" onClick={() => setAddFormOpen(true)}>
              첫 에셋 추가하기
            </button>
          </div>
        )}
        <ReactFlow
          nodes={highlightedNodes}
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

        {showAssetPanel && selectedNode && (
          <SidePanel
            node={selectedNode}
            objectTypeSchemas={objectTypeSchemas}
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
                    ? {
                        ...n,
                        data: {
                          asset: { ...asset, type: type || asset.type, metadata },
                        },
                      }
                    : n,
                ),
              )
              setSelectedNodeId(null)
            }}
            onDeleted={async () => {
              const id = selectedNode.id
              await deleteAsset(id)
              setNodes((nds) => nds.filter((n) => n.id !== id))
              setEdges((eds) =>
                eds.filter((e) => e.source !== id && e.target !== id),
              )
              setSelectedNodeId(null)
            }}
          />
        )}

        {showRelEditPanel && selectedRelationship && (
          <RelationshipEditPanel
            relationship={selectedRelationship}
            linkTypeSchemas={linkTypeSchemas}
            onClose={() => setSelectedEdgeId(null)}
            onSaved={(updated) => {
              setEdges((eds) =>
                eds.map((e) =>
                  e.id === updated.id ? { ...e, data: { relationship: updated } } : e,
                ),
              )
              setSelectedEdgeId(null)
            }}
            onDeleted={(id) => {
              setEdges((eds) => eds.filter((e) => e.id !== id))
              setSelectedEdgeId(null)
            }}
          />
        )}

        {showCreatePanel && (
          <CreateRelationshipPanel
            sourceId={relSourceId}
            targetId={relTargetId}
            nodes={nodes}
            linkTypeSchemas={linkTypeSchemas}
            objectTypeSchemas={objectTypeSchemas}
            onSetSource={setRelSourceId}
            onSetTarget={setRelTargetId}
            onSwap={() => {
              setRelSourceId(relTargetId)
              setRelTargetId(relSourceId)
            }}
            onClose={exitRelMode}
            onCreated={() => {
              exitRelMode()
              loadData()
            }}
          />
        )}

        {showObjectTypePanel && (
          <ObjectTypePanel
            schemas={objectTypeSchemas}
            onClose={() => setObjectTypePanelOpen(false)}
            onRefresh={refreshObjectTypeSchemas}
          />
        )}

        {showSimPanel && (
          <SimulationPanel
            assets={assets}
            selectedAssetId={selectedNodeId}
            onClose={() => setSimPanelOpen(false)}
            onAssetStateUpdate={(assetId, properties, status) => {
              setNodes((nds) =>
                nds.map((n) => {
                  if (n.id !== assetId) return n
                  return {
                    ...n,
                    data: {
                      ...n.data,
                      liveStatus: status,
                      liveProperties: properties,
                    },
                  }
                }),
              )
            }}
          />
        )}
      </div>

      {addFormOpen && (
        <AddAssetModal
          objectTypeSchemas={objectTypeSchemas}
          onClose={() => setAddFormOpen(false)}
          onCreated={() => {
            setAddFormOpen(false)
            loadData()
          }}
        />
      )}
    </div>
  )
}

/* ---- ObjectTypePanel ---- */

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

function ObjectTypePanel({
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
    <div className="assets-canvas-page__side-panel assets-canvas-page__object-type-panel">
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
    </div>
  )
}

/* ---- CreateRelationshipPanel (slide-in side panel, 3-step wizard) ---- */

function CreateRelationshipPanel({
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
  nodes: Node<AssetNodeData>[]
  linkTypeSchemas: LinkTypeSchemaDto[]
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onSetSource: (id: string | null) => void
  onSetTarget: (id: string | null) => void
  onSwap: () => void
  onClose: () => void
  onCreated: () => void
}) {
  const [selectedLinkType, setSelectedLinkType] = useState('')
  const [transferKeys, setTransferKeys] = useState<string[]>([])
  const [ratio, setRatio] = useState('1')
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

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
      .map((key) => ({ key, source: 'metadata' as const }))
  }, [sourceAsset, eligibleProps])

  const bothSelected = sourceId != null && targetId != null
  const canProceedToType = bothSelected
  const canProceedToProps = canProceedToType && selectedLinkType !== ''
  const hasTransfersProperty = linkSchema?.properties.some((p) => p.key === 'transfers') ?? false

  const step = !bothSelected ? 1 : selectedLinkType === '' ? 2 : 3

  const assetLabel = (id: string | null) => {
    if (!id) return '(캔버스에서 클릭)'
    const node = nodes.find((n) => n.id === id)
    return node ? `${node.data.asset.type} (${id.slice(0, 8)}...)` : id
  }

  const handleSubmit = async () => {
    if (!sourceId || !targetId || !selectedLinkType) return
    setCreateError(null)
    setSubmitting(true)
    try {
      const properties: Record<string, unknown> = {}
      if (hasTransfersProperty && transferKeys.length > 0) {
        properties.transfers = transferKeys.map((key) => ({
          key,
          ratio: parseFloat(ratio) || 1,
        }))
      }
      if (linkSchema) {
        const ratioSchema = linkSchema.properties.find((p) => p.key === 'ratio')
        if (ratioSchema) {
          properties.ratio = parseFloat(ratio) || 1
        }
      }
      const body: CreateRelationshipRequest = {
        fromAssetId: sourceId,
        toAssetId: targetId,
        relationshipType: selectedLinkType,
        properties,
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
    <div className="assets-canvas-page__side-panel assets-canvas-page__create-rel-panel">
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

      {/* Step 1: Asset Selection */}
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

      {/* Step 2: Link Type Selection */}
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

      {/* Step 3: Properties */}
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

          {linkSchema?.properties.some((p) => p.key === 'ratio') && (
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

          {createError && <p className="assets-canvas-page__error">{createError}</p>}

          <button
            type="button"
            className="create-rel__submit-btn"
            disabled={submitting}
            onClick={handleSubmit}
          >
            {submitting ? '생성 중...' : '관계 생성'}
          </button>
        </div>
      )}
    </div>
  )
}

/* ---- SidePanel (asset edit) ---- */

function SidePanel({
  node,
  objectTypeSchemas,
  onClose,
  onSave,
  onDeleted,
}: {
  node: Node<AssetNodeData>
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onSave: (type: string, metadata: Record<string, unknown>) => Promise<void>
  onDeleted: () => Promise<void>
}) {
  const asset = node.data.asset
  const [type, setType] = useState(asset.type)
  const [metadata, setMetadata] = useState<Record<string, unknown>>(() =>
    mergeAssetMetadataWithSchema(
      asset.type,
      objectTypeSchemas,
      (asset.metadata ?? {}) as Record<string, unknown>,
    ),
  )
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [showSystemInfo, setShowSystemInfo] = useState(false)

  const schema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )
  const extraKeys = useMemo(
    () => Object.keys(metadata).filter((k) => !schemaKeySet.has(k)),
    [metadata, schemaKeySet],
  )

  const setMetaValue = (key: string, raw: string) => {
    setMetadata((m) => ({ ...m, [key]: raw }))
  }

  const removeExtraKey = (key: string) => {
    setMetadata((m) => {
      const next = { ...m }
      delete next[key]
      return next
    })
  }

  const addExtraRow = () => {
    const k = `extra_${Date.now()}`
    setMetadata((m) => ({ ...m, [k]: '' }))
  }

  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => buildMetadataFromTypeSelection(newType, objectTypeSchemas, prev))
  }

  const copyId = () => {
    void navigator.clipboard.writeText(asset.id)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaveError(null)
    setSaving(true)
    try {
      await onSave(type.trim(), metadata)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : '저장 실패')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!window.confirm('이 에셋을 삭제할까요? 이 작업은 되돌릴 수 없습니다.')) return
    setDeleteError(null)
    setDeleting(true)
    try {
      await onDeleted()
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : '삭제 실패')
    } finally {
      setDeleting(false)
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
          ObjectType
          <select
            value={type}
            onChange={(e) => handleTypeChange(e.target.value)}
            required
            aria-label="ObjectType"
          >
            <option value="">— 선택 —</option>
            {objectTypeSchemas.map((s) => (
              <option key={s.objectType} value={s.objectType}>
                {s.displayName} ({s.objectType})
              </option>
            ))}
            {type && !objectTypeSchemas.some((s) => s.objectType === type) ? (
              <option value={type}>{type} (스키마 없음)</option>
            ) : null}
          </select>
        </label>
        {schemaProps.length > 0 && (
          <div className="assets-canvas-page__meta-section">
            <span>스키마 속성</span>
            {schemaProps.map((p) => (
              <div key={p.key} className="assets-canvas-page__meta-row">
                <input value={p.key} readOnly aria-label={`${p.key}-key`} />
                <input
                  value={String(metadata[p.key] ?? '')}
                  readOnly={p.mutability === 'Immutable'}
                  onChange={
                    p.mutability === 'Mutable'
                      ? (e) => setMetaValue(p.key, e.target.value)
                      : undefined
                  }
                  aria-label={`${p.key}-value`}
                />
                <span className="assets-canvas-page__prop-badge">
                  {p.dataType} / {p.simulationBehavior} / {p.mutability}
                </span>
              </div>
            ))}
          </div>
        )}
        <div className="assets-canvas-page__meta-section">
          <span>추가 메타데이터 (스키마 외 키)</span>
          {extraKeys.map((key) => (
            <div key={key} className="assets-canvas-page__meta-row">
              <input
                placeholder="key"
                value={key}
                readOnly
                aria-label="extra-key"
              />
              <input
                placeholder="value"
                value={String(metadata[key] ?? '')}
                onChange={(e) => setMetaValue(key, e.target.value)}
              />
              <button type="button" onClick={() => removeExtraKey(key)}>
                삭제
              </button>
            </div>
          ))}
          <button type="button" onClick={addExtraRow}>
            항목 추가
          </button>
        </div>
        {saveError && <p className="assets-canvas-page__error">{saveError}</p>}
        <button type="submit" disabled={saving}>
          {saving ? '저장 중…' : '저장'}
        </button>
      </form>
      <div className="assets-canvas-page__side-panel-danger">
        {deleteError && <p className="assets-canvas-page__error">{deleteError}</p>}
        <button
          type="button"
          className="assets-canvas-page__delete-asset-btn"
          disabled={deleting}
          onClick={() => void handleDelete()}
        >
          {deleting ? '삭제 중…' : '에셋 삭제'}
        </button>
      </div>
    </div>
  )
}

/* ---- AddAssetModal ---- */

function AddAssetModal({
  objectTypeSchemas,
  onClose,
  onCreated,
}: {
  objectTypeSchemas: ObjectTypeSchemaDto[]
  onClose: () => void
  onCreated: () => void
}) {
  const [type, setType] = useState('')
  const [metadata, setMetadata] = useState<Record<string, unknown>>({})
  const [submitting, setSubmitting] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const selectedSchema = objectTypeSchemas.find((s) => s.objectType === type) ?? null
  const schemaProps = selectedSchema?.resolvedProperties ?? selectedSchema?.ownProperties ?? []
  const schemaKeySet = useMemo(
    () => new Set(schemaProps.map((p) => p.key)),
    [schemaProps],
  )
  const extraKeys = useMemo(
    () => Object.keys(metadata).filter((k) => !schemaKeySet.has(k)),
    [metadata, schemaKeySet],
  )

  const handleTypeChange = (newType: string) => {
    setType(newType)
    setMetadata((prev) => buildMetadataFromTypeSelection(newType, objectTypeSchemas, prev))
  }

  const setMetaValue = (key: string, raw: string) => {
    setMetadata((m) => ({ ...m, [key]: raw }))
  }

  const removeExtraKey = (key: string) => {
    setMetadata((m) => {
      const next = { ...m }
      delete next[key]
      return next
    })
  }

  const addExtraRow = () => {
    const k = `extra_${Date.now()}`
    setMetadata((m) => ({ ...m, [k]: '' }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)
    const t = type.trim()
    if (!t) {
      setCreateError('ObjectType을 선택하세요')
      return
    }
    setSubmitting(true)
    try {
      await createAsset({
        type: t,
        connections: [],
        metadata: Object.keys(metadata).length ? metadata : {},
      })
      onCreated()
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
            ObjectType <span className="required">*</span>
            <select value={type} onChange={(e) => handleTypeChange(e.target.value)} required>
              <option value="">— 선택 —</option>
              {objectTypeSchemas.map((schema) => (
                <option key={schema.objectType} value={schema.objectType}>
                  {schema.displayName} ({schema.objectType})
                </option>
              ))}
            </select>
          </label>
          {schemaProps.length > 0 && (
            <div className="assets-canvas-page__meta-section">
              <span>스키마 속성</span>
              {schemaProps.map((p) => (
                <div key={p.key} className="assets-canvas-page__meta-row">
                  <input value={p.key} readOnly aria-label={`schema-${p.key}-key`} />
                  <input
                    value={String(metadata[p.key] ?? '')}
                    readOnly={p.mutability === 'Immutable'}
                    onChange={
                      p.mutability === 'Mutable'
                        ? (e) => setMetaValue(p.key, e.target.value)
                        : undefined
                    }
                    aria-label={`schema-${p.key}-value`}
                  />
                </div>
              ))}
            </div>
          )}
          <div className="assets-canvas-page__meta-section">
            <span>추가 메타데이터</span>
            {extraKeys.map((key) => (
              <div key={key} className="assets-canvas-page__meta-row">
                <input placeholder="key" value={key} readOnly />
                <input
                  placeholder="value"
                  value={String(metadata[key] ?? '')}
                  onChange={(e) => setMetaValue(key, e.target.value)}
                />
                <button type="button" onClick={() => removeExtraKey(key)}>
                  삭제
                </button>
              </div>
            ))}
            <button type="button" onClick={addExtraRow}>
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

/* ---- SimulationPanel (side panel) ---- */

function SimulationPanel({
  assets,
  selectedAssetId,
  onClose,
  onAssetStateUpdate,
}: {
  assets: AssetDto[]
  selectedAssetId: string | null
  onClose: () => void
  onAssetStateUpdate: (assetId: string, properties: Record<string, unknown>, status: string) => void
}) {
  const [mode, setMode] = useState<'single' | 'continuous'>('single')
  const [triggerAssetId, setTriggerAssetId] = useState(selectedAssetId ?? '')
  const [maxDepth, setMaxDepth] = useState('5')
  const [running, setRunning] = useState(false)
  const [activeRunId, setActiveRunId] = useState<string | null>(null)
  const [events, setEvents] = useState<EventDto[]>([])
  const [resultMessage, setResultMessage] = useState<string | null>(null)
  const [simError, setSimError] = useState<string | null>(null)
  const [sseCleanup, setSseCleanup] = useState<(() => void) | null>(null)
  const [tickCount, setTickCount] = useState(0)

  useEffect(() => {
    if (selectedAssetId) setTriggerAssetId(selectedAssetId)
  }, [selectedAssetId])

  useEffect(() => {
    return () => {
      sseCleanup?.()
    }
  }, [sseCleanup])

  const startSseSubscription = useCallback(() => {
    const cleanup = subscribeSimulationEvents((tickEvent) => {
      onAssetStateUpdate(tickEvent.assetId, tickEvent.properties, tickEvent.status)
      setTickCount((c) => c + 1)
    })
    setSseCleanup(() => cleanup)
  }, [onAssetStateUpdate])

  const handleSingleRun = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    setRunning(true)
    try {
      const result = await runSimulation({
        triggerAssetId,
        maxDepth: parseInt(maxDepth) || 5,
      })
      setResultMessage(result.message)
      if (result.runId) {
        const evts = await getRunEvents(result.runId)
        setEvents(evts)
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 실패')
    } finally {
      setRunning(false)
    }
  }

  const handleStartContinuous = async () => {
    if (!triggerAssetId) return
    setSimError(null)
    setResultMessage(null)
    setEvents([])
    setTickCount(0)
    setRunning(true)
    try {
      const result = await startContinuousRun({
        triggerAssetId,
        maxDepth: parseInt(maxDepth) || 5,
      })
      if (result.success && result.runId) {
        setActiveRunId(result.runId)
        setResultMessage(`지속 실행 시작: ${result.runId}`)
        startSseSubscription()
      } else {
        setSimError(result.message ?? '시작 실패')
        setRunning(false)
      }
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '시뮬레이션 시작 실패')
      setRunning(false)
    }
  }

  const handleStop = async () => {
    if (!activeRunId) return
    try {
      await stopRun(activeRunId)
      sseCleanup?.()
      setSseCleanup(null)
      setResultMessage('시뮬레이션 중지됨')
    } catch (err) {
      setSimError(err instanceof Error ? err.message : '중지 실패')
    } finally {
      setRunning(false)
      setActiveRunId(null)
    }
  }

  return (
    <div className="assets-canvas-page__side-panel assets-canvas-page__sim-panel">
      <div className="assets-canvas-page__side-panel-header">
        <h3>시뮬레이션</h3>
        <button type="button" onClick={onClose} aria-label="닫기">
          ×
        </button>
      </div>

      <div className="sim-panel__mode-toggle">
        <button
          type="button"
          className={mode === 'single' ? 'active' : ''}
          onClick={() => setMode('single')}
          disabled={running}
        >
          1회 실행
        </button>
        <button
          type="button"
          className={mode === 'continuous' ? 'active' : ''}
          onClick={() => setMode('continuous')}
          disabled={running}
        >
          지속 실행
        </button>
      </div>

      <div className="sim-panel__section">
        <label>
          트리거 에셋
          <select
            value={triggerAssetId}
            onChange={(e) => setTriggerAssetId(e.target.value)}
            disabled={running}
          >
            <option value="">선택하세요...</option>
            {assets.map((a) => (
              <option key={a.id} value={a.id}>
                {a.type} ({a.id.slice(0, 8)}...)
              </option>
            ))}
          </select>
        </label>
        <label>
          최대 전파 깊이
          <input
            type="number"
            min="1"
            max="20"
            value={maxDepth}
            onChange={(e) => setMaxDepth(e.target.value)}
            disabled={running}
          />
        </label>
      </div>

      <div className="sim-panel__actions">
        {!running ? (
          mode === 'single' ? (
            <button
              type="button"
              onClick={handleSingleRun}
              disabled={!triggerAssetId}
              className="sim-panel__run-btn"
            >
              실행
            </button>
          ) : (
            <button
              type="button"
              onClick={handleStartContinuous}
              disabled={!triggerAssetId}
              className="sim-panel__run-btn"
            >
              시작
            </button>
          )
        ) : (
          <button
            type="button"
            onClick={handleStop}
            className="sim-panel__stop-btn"
          >
            중지
          </button>
        )}
      </div>

      {simError && <p className="assets-canvas-page__error">{simError}</p>}
      {resultMessage && <p className="sim-panel__result">{resultMessage}</p>}

      {running && activeRunId && (
        <div className="sim-panel__live-indicator">
          <span className="sim-panel__live-dot" />
          LIVE — tick 수신: {tickCount}
        </div>
      )}

      {events.length > 0 && (
        <div className="sim-panel__events">
          <span className="sim-panel__events-title">이벤트 ({events.length}건)</span>
          <ul className="sim-panel__events-list">
            {events.map((evt, i) => (
              <li key={i} className="sim-panel__event-item">
                <span className="sim-panel__event-type">{evt.eventType}</span>
                <span className="sim-panel__event-asset">{evt.assetId}</span>
                <time>{new Date(evt.occurredAt).toLocaleTimeString()}</time>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

/* ---- RelationshipEditPanel (existing edge edit) ---- */

export function RelationshipEditPanel({
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
