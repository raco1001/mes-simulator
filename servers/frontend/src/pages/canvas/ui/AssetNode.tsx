import { Handle, Position, type Node, type NodeProps } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'
import './AssetNode.css'

export type AssetNodeData = {
  asset: AssetDto
  liveStatus?: string
  liveProperties?: Record<string, unknown>
}

function metadataSummary(meta: Record<string, unknown> | undefined): string {
  if (!meta || Object.keys(meta).length === 0) return '-'
  return Object.entries(meta)
    .slice(0, 2)
    .map(([k, v]) => `${k}: ${v}`)
    .join(', ')
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

  const live = data.liveProperties
  const hasLiveEntries = live && Object.keys(live).length > 0

  return (
    <>
      <Handle type="target" position={Position.Left} />
      <div className={`asset-node ${liveClass}`}>
        <div className="asset-node__type">{asset.type}</div>
        {hasLiveEntries ? (
          <div className="asset-node__props">
            {Object.entries(live).map(([k, v]) => (
              <div key={k} className="asset-node__prop-line">
                <span className="asset-node__prop-key">{k}:</span>{' '}
                <span className="asset-node__prop-val">{formatPropValue(v)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div className="asset-node__meta">{metadataSummary(asset.metadata)}</div>
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
