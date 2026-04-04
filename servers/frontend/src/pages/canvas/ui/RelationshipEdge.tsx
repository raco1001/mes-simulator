import { BaseEdge, getBezierPath, type Edge, type EdgeProps } from '@xyflow/react'
import type { RelationshipDto } from '@/entities/relationship'

export type RelationshipEdgeData = { relationship: RelationshipDto }
export type RelationshipEdge = Edge<RelationshipEdgeData>

export function RelationshipEdgeComponent(props: EdgeProps<RelationshipEdge>) {
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
