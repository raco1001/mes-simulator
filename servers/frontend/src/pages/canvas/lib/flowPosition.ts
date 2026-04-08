import { CANVAS_POSITION_KEY } from '@/shared/lib/canvasMetadata'

export type FlowPosition = { x: number; y: number }

function isFiniteNum(v: unknown): v is number {
  return typeof v === 'number' && Number.isFinite(v)
}

/**
 * Reads React Flow position from asset metadata.canvasPosition (well-known key).
 */
export function parseCanvasPositionFromMetadata(
  meta: Record<string, unknown> | undefined,
): FlowPosition | null {
  if (!meta) return null
  const keys = Object.keys(meta)
  const k =
    keys.find((key) => key.toLowerCase() === CANVAS_POSITION_KEY.toLowerCase()) ??
    CANVAS_POSITION_KEY
  const raw = meta[k]
  if (!raw || typeof raw !== 'object') return null
  const o = raw as Record<string, unknown>
  const x = o.x
  const y = o.y
  if (!isFiniteNum(x) || !isFiniteNum(y)) return null
  return { x, y }
}

export function getGridFallbackPosition(
  index: number,
  gridX: number,
  gridY: number,
): FlowPosition {
  return { x: index * gridX, y: index * gridY }
}
