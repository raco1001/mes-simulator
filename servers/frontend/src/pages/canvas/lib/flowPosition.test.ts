import { describe, it, expect } from 'vitest'
import {
  getGridFallbackPosition,
  parseCanvasPositionFromMetadata,
} from './flowPosition'
import {
  CANVAS_POSITION_KEY,
  isHiddenFromFlatMetadataKeys,
} from '@/shared/lib/canvasMetadata'

describe('parseCanvasPositionFromMetadata', () => {
  it('returns null for empty metadata', () => {
    expect(parseCanvasPositionFromMetadata(undefined)).toBeNull()
  })

  it('parses valid canvasPosition', () => {
    expect(
      parseCanvasPositionFromMetadata({
        [CANVAS_POSITION_KEY]: { x: 10, y: -20 },
      }),
    ).toEqual({ x: 10, y: -20 })
  })

  it('accepts camelCase key variant', () => {
    expect(
      parseCanvasPositionFromMetadata({
        CanvasPosition: { x: 1, y: 2 },
      }),
    ).toEqual({ x: 1, y: 2 })
  })

  it('returns null for invalid numbers', () => {
    expect(
      parseCanvasPositionFromMetadata({
        [CANVAS_POSITION_KEY]: { x: NaN, y: 0 },
      }),
    ).toBeNull()
  })
})

describe('getGridFallbackPosition', () => {
  it('returns staggered grid', () => {
    expect(getGridFallbackPosition(2, 220, 140)).toEqual({ x: 440, y: 280 })
  })
})

describe('canvasPosition metadata key', () => {
  it('is hidden from flat metadata', () => {
    expect(isHiddenFromFlatMetadataKeys('canvasPosition')).toBe(true)
  })
})
