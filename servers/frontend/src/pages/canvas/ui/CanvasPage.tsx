import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type MouseEvent,
} from 'react'
import {
  ReactFlow,
  Background,
  BackgroundVariant,
  useNodesState,
  useEdgesState,
  type Node,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import {
  getAssets,
  updateAsset,
  deleteAsset,
  type AssetDto,
} from '@/entities/asset'
import {
  getObjectTypeSchemas,
  type ObjectTypeSchemaDto,
} from '@/entities/object-type-schema'
import {
  getLinkTypeSchemas,
  type LinkTypeSchemaDto,
} from '@/entities/link-type-schema'
import { getRelationships } from '@/entities/relationship'
import { EDGE_TYPE, GRID_X, GRID_Y, NODE_TYPE } from '../lib/canvasConstants'
import {
  CANVAS_THEME_STORAGE_KEY,
  getInitialCanvasTheme,
  type CanvasTheme,
} from '../lib/canvasTheme'
import type { AssetNodeData } from './AssetNode'
import { AssetNode } from './AssetNode'
import {
  RelationshipEdgeComponent,
  type RelationshipEdge,
} from './RelationshipEdge'
import { CanvasSidePanel } from '@/widgets/canvas-side-panel'
import { EditAssetOnPanel } from '@/features/edit-asset-on-panel'
import { CreateRelationshipOnPanel } from '@/features/edit-relationship-on-panel'
import { EditTypeOnPanel } from '@/features/edit-type-on-panel'
import { RunSimulationOnPanel } from '@/features/run-simulation-on-panel'
import { CanvasToolbar } from './CanvasToolbar'
import { CanvasOnboarding } from './CanvasOnboarding'
import { AddAssetModal } from './AddAssetModal'
import './canvasTheme.css'
import './CanvasPage.css'

export function CanvasPage() {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node<AssetNodeData>>(
    [],
  )
  const [edges, setEdges, onEdgesChange] = useEdgesState<RelationshipEdge>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [objectTypeSchemas, setObjectTypeSchemas] = useState<
    ObjectTypeSchemaDto[]
  >([])
  const [linkTypeSchemas, setLinkTypeSchemas] = useState<LinkTypeSchemaDto[]>(
    [],
  )
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null)
  const [selectedEdgeId, setSelectedEdgeId] = useState<string | null>(null)
  const [addFormOpen, setAddFormOpen] = useState(false)

  const [relMode, setRelMode] = useState(false)
  const [relSourceId, setRelSourceId] = useState<string | null>(null)
  const [relTargetId, setRelTargetId] = useState<string | null>(null)
  const [simPanelOpen, setSimPanelOpen] = useState(false)
  const [objectTypePanelOpen, setObjectTypePanelOpen] = useState(false)
  const [canvasTheme, setCanvasTheme] = useState<CanvasTheme>(
    getInitialCanvasTheme,
  )

  useEffect(() => {
    try {
      window.localStorage.setItem(CANVAS_THEME_STORAGE_KEY, canvasTheme)
    } catch {
      /* ignore quota / private mode */
    }
  }, [canvasTheme])

  const toggleCanvasTheme = useCallback(() => {
    setCanvasTheme((t) => (t === 'light' ? 'dark' : 'light'))
  }, [])

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
    (_: MouseEvent, node: Node<AssetNodeData>) => {
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
    (_: MouseEvent, edge: RelationshipEdge) => {
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
      else if (relMode && n.id === relTargetId)
        className = 'rel-target-highlight'
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

  const editingRelationship =
    !relMode &&
    !simPanelOpen &&
    !objectTypePanelOpen &&
    selectedRelationship != null
      ? selectedRelationship
      : null

  const showRelationshipPanel =
    !simPanelOpen &&
    !objectTypePanelOpen &&
    (relMode || editingRelationship != null)

  const showAssetPanel =
    !relMode && !simPanelOpen && !objectTypePanelOpen && selectedNode != null
  const showSimPanel = simPanelOpen && !relMode && !objectTypePanelOpen
  const showObjectTypePanel = objectTypePanelOpen && !relMode

  const assets: AssetDto[] = useMemo(
    () => nodes.map((n) => n.data.asset),
    [nodes],
  )

  if (loading) {
    return (
      <div
        className="assets-canvas-page"
        data-canvas-theme={canvasTheme}
      >
        Loading...
      </div>
    )
  }
  if (error) {
    return (
      <div
        className="assets-canvas-page"
        data-canvas-theme={canvasTheme}
      >
        Error: {error}
      </div>
    )
  }

  return (
    <div
      className={`assets-canvas-page ${relMode ? 'assets-canvas-page--rel-mode' : ''}`}
      data-canvas-theme={canvasTheme}
    >
      <CanvasToolbar
        relMode={relMode}
        objectTypePanelOpen={objectTypePanelOpen}
        simPanelOpen={simPanelOpen}
        nodeCount={nodes.length}
        canvasTheme={canvasTheme}
        onToggleCanvasTheme={toggleCanvasTheme}
        onAddAsset={() => setAddFormOpen(true)}
        onEnterRelMode={enterRelMode}
        onExitRelMode={exitRelMode}
        onToggleObjectTypePanel={toggleObjectTypePanel}
        onToggleSimPanel={toggleSimPanel}
      />

      <div className="assets-canvas-page__main">
        {isEmpty && (
          <CanvasOnboarding onAddFirstAsset={() => setAddFormOpen(true)} />
        )}
        <ReactFlow
          proOptions={{ hideAttribution: true }}
          colorMode={canvasTheme}
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
          <Background
            id="canvas-dots"
            variant={BackgroundVariant.Dots}
            gap={16}
            size={1}
          />
        </ReactFlow>

        {showAssetPanel && selectedNode && (
          <CanvasSidePanel>
            <EditAssetOnPanel
              asset={(selectedNode.data as AssetNodeData).asset}
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
                            asset: {
                              ...asset,
                              type: type || asset.type,
                              metadata,
                            },
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
          </CanvasSidePanel>
        )}

        {showRelationshipPanel && (
          <CreateRelationshipOnPanel
            key={
              editingRelationship
                ? `edit-${editingRelationship.id}`
                : 'create-rel'
            }
            editingRelationship={editingRelationship}
            sourceId={
              editingRelationship
                ? editingRelationship.fromAssetId
                : relSourceId
            }
            targetId={
              editingRelationship ? editingRelationship.toAssetId : relTargetId
            }
            nodes={nodes}
            linkTypeSchemas={linkTypeSchemas}
            objectTypeSchemas={objectTypeSchemas}
            onSetSource={setRelSourceId}
            onSetTarget={setRelTargetId}
            onSwap={() => {
              setRelSourceId(relTargetId)
              setRelTargetId(relSourceId)
            }}
            onClose={() => {
              if (editingRelationship) setSelectedEdgeId(null)
              else exitRelMode()
            }}
            onCreated={() => {
              exitRelMode()
              loadData()
            }}
            onSaved={(updated) => {
              setEdges((eds) =>
                eds.map((e) =>
                  e.id === updated.id
                    ? { ...e, data: { relationship: updated } }
                    : e,
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

        {showObjectTypePanel && (
          <CanvasSidePanel className="assets-canvas-page__object-type-panel">
            <EditTypeOnPanel
              schemas={objectTypeSchemas}
              onClose={() => setObjectTypePanelOpen(false)}
              onRefresh={refreshObjectTypeSchemas}
            />
          </CanvasSidePanel>
        )}

        {showSimPanel && (
          <RunSimulationOnPanel
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
