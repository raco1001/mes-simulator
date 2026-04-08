import { describe, it, expect } from 'vitest'
import {
  formatAssetMetadataSummary,
  formatMetadataValuePreview,
  getAssetDisplayTitle,
} from './assetDisplay'
import { getAssetLabel, type AssetDto } from '@/entities/asset'

describe('getAssetDisplayTitle', () => {
  it('uses assetName when set', () => {
    expect(
      getAssetDisplayTitle({
        type: 'freezer',
        metadata: { assetName: 'Cold-1' },
      }),
    ).toBe('Cold-1')
  })

  it('falls back to type', () => {
    expect(getAssetDisplayTitle({ type: 'freezer', metadata: {} })).toBe(
      'freezer',
    )
  })
})

describe('formatMetadataValuePreview', () => {
  it('summarizes extra-like object arrays by key field', () => {
    expect(
      formatMetadataValuePreview([
        { key: 'a', dataType: 'Number' },
        { key: 'b', value: 1 },
        { key: 'c' },
        { key: 'd' },
      ]),
    ).toBe('a, b, c, …')
  })

  it('shows item count for generic arrays', () => {
    expect(formatMetadataValuePreview([1, 2, 3])).toBe('(3 items)')
  })
})

describe('formatAssetMetadataSummary', () => {
  it('skips assetName and extraProperties', () => {
    expect(
      formatAssetMetadataSummary({
        assetName: 'X',
        extraProperties: [{ key: 'k' }],
        capacity: '100',
      }),
    ).toBe('capacity: 100')
  })

  it('skips extra_properties snake_case', () => {
    expect(
      formatAssetMetadataSummary({
        extra_properties: [{ key: 'p' }],
        note: 'ok',
      }),
    ).toBe('note: ok')
  })
})

describe('getAssetLabel', () => {
  it('includes shortened id', () => {
    const asset: AssetDto = {
      id: 'asset-1',
      type: 'freezer',
      connections: [],
      metadata: { assetName: 'F1' },
      createdAt: '',
      updatedAt: '',
    }
    expect(getAssetLabel(asset)).toBe('F1 (asset-1)')
  })
})
