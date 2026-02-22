import { Handle, Position, type Node, type NodeProps } from '@xyflow/react'
import type { AssetDto } from '@/entities/asset'

export type AssetNodeData = {
  asset: AssetDto
}

function metadataSummary(meta: Record<string, unknown> | undefined): string {
  if (!meta || Object.keys(meta).length === 0) return '-'
  return Object.entries(meta)
    .slice(0, 2)
    .map(([k, v]) => `${k}: ${v}`)
    .join(', ')
}

export function AssetNode(props: NodeProps<Node<AssetNodeData, 'asset'>>) {
  const { data } = props
  const asset = data?.asset
  if (!asset) return null

  return (
    <>
      <Handle type="target" position={Position.Left} />
      <div className="asset-node">
        <div className="asset-node__type">{asset.type}</div>
        <div className="asset-node__meta">{metadataSummary(asset.metadata)}</div>
      </div>
      <Handle type="source" position={Position.Right} />
    </>
  )
}
