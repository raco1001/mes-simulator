import { Handle, Position, type Node, type NodeProps } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'

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

  return (
    <>
      <Handle type="target" position={Position.Left} />
      <div className={`asset-node ${liveClass}`}>
        <div className="asset-node__type">{asset.type}</div>
        <div className="asset-node__meta">{metadataSummary(data.liveProperties ?? asset.metadata)}</div>
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
