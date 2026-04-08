import { BaseEdge, getBezierPath, type Edge, type EdgeProps } from '@xyflow/react'
import type { RelationshipDto } from '@/entities/relationship'
import type { SimCanvasPhase } from '@/pages/canvas/lib/useCanvasSimulationSync'

export type RelationshipEdgeData = {
  relationship: RelationshipDto
  simCanvasPhase?: SimCanvasPhase
}
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
    data,
  } = props
  const [path] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  })
  const phase = data?.simCanvasPhase ?? 'idle'
  const flowRunning = phase === 'running'
  return (
    <g data-testid="relationship-edge" data-sim-phase={phase}>
      <BaseEdge
        id={id}
        path={path}
        className={flowRunning ? 'relationship-edge-path--flow' : undefined}
        style={{
          stroke: 'var(--canvas-edge-stroke, #94a3b8)',
          strokeWidth: 1.5,
          strokeDasharray: flowRunning ? '8 6' : undefined,
        }}
      />
    </g>
  )
}
