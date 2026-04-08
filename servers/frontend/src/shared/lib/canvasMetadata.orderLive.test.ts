import { describe, it, expect } from 'vitest'
import { orderLivePropertiesForDisplay } from './canvasMetadata'

describe('orderLivePropertiesForDisplay', () => {
  it('puts streamCapacity and capacity before streamInput* then others A–Z', () => {
    const ordered = orderLivePropertiesForDisplay(
      Object.entries({
        streamInput_b: 2,
        zOther: 9,
        streamCapacity: 500,
        streamInput_a: 1,
        capacity: 999,
        alpha: 0,
      }),
    )
    expect(ordered.map(([k]) => k)).toEqual([
      'streamCapacity',
      'capacity',
      'streamInput_a',
      'streamInput_b',
      'alpha',
      'zOther',
    ])
  })

  it('drops canvasPosition from live node display', () => {
    const ordered = orderLivePropertiesForDisplay(
      Object.entries({
        canvasPosition: { x: 1, y: 2 },
        streamOut: 3500,
      }),
    )
    expect(ordered.map(([k]) => k)).toEqual(['streamOut'])
  })

  it('drops canvas_position and assetName variants from live display', () => {
    const ordered = orderLivePropertiesForDisplay(
      Object.entries({
        canvas_position: { x: 0, y: 0 },
        asset_name: 'Drone 1',
        stream: 100,
      }),
    )
    expect(ordered.map(([k]) => k)).toEqual(['stream'])
  })
})
