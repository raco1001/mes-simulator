import { Handle, Position, type Node, type NodeProps } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'
import {
  formatAssetMetadataSummary,
  getAssetDisplayTitle,
} from '@/shared/lib/assetDisplay'
import { orderLivePropertiesForDisplay } from '@/shared/lib/canvasMetadata'
import type { SimCanvasPhase } from '@/pages/canvas/lib/useCanvasSimulationSync'
import './AssetNode.css'

export type AssetNodeData = {
  asset: AssetDto
  liveStatus?: string
  liveProperties?: Record<string, unknown>
  simCanvasPhase?: SimCanvasPhase
}

function formatPropValue(v: unknown): string {
  if (v == null) return '—'
  return String(v)
}

function statusClass(status?: string): string {
  if (!status) return ''
  const s = status.toLowerCase()
  if (s === 'warning') return 'asset-node--warning'
  if (s === 'error' || s === 'critical') return 'asset-node--error'
  return ''
}

export function AssetNode(props: NodeProps<Node<AssetNodeData, 'asset'>>) {
  const { data } = props
  const asset = data?.asset
  if (!asset) return null

  const liveClass = statusClass(data.liveStatus)

  const phase = data.simCanvasPhase ?? 'idle'
  const phaseClass =
    phase === 'idle'
      ? 'asset-node--phase-idle'
      : phase === 'stoppedCached'
        ? 'asset-node--phase-stopped'
        : 'asset-node--phase-running'
  const runningOkClass =
    phase === 'running' && !liveClass ? 'asset-node--live-ok' : ''

  const live = data.liveProperties
  const liveEntries =
    live && Object.keys(live).length > 0
      ? orderLivePropertiesForDisplay(Object.entries(live))
      : []
  const hasLiveEntries = liveEntries.length > 0
  const displayTitle = getAssetDisplayTitle(asset)
  const showTypeCode = displayTitle !== asset.type

  return (
    <>
      <Handle type="target" position={Position.Left} />
      <div
        className={`asset-node ${liveClass} ${phaseClass} ${runningOkClass}`}
      >
        <div className="asset-node__type">{displayTitle}</div>
        {showTypeCode ? (
          <div className="asset-node__type-code" title={asset.id}>
            {asset.type}
          </div>
        ) : null}
        {hasLiveEntries ? (
          <div className="asset-node__props">
            {liveEntries.map(([k, v]) => (
              <div key={k} className="asset-node__prop-line">
                <span className="asset-node__prop-key">{k}:</span>{' '}
                <span className="asset-node__prop-val">{formatPropValue(v)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div className="asset-node__meta">
            {formatAssetMetadataSummary(asset.metadata)}
          </div>
        )}
        {data.liveStatus && (
          <div className={`asset-node__status asset-node__status--${data.liveStatus.toLowerCase()}`}>
            {data.liveStatus}
          </div>
        )}
      </div>
      <Handle type="source" position={Position.Right} />
    </>
  )
}
