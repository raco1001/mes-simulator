import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from '@/shared/api'
import {
  getAssets,
  getAssetById,
  createAsset,
  updateAsset,
} from './assetApi'
import type { AssetDto, CreateAssetRequest, UpdateAssetRequest } from '../model/types'

vi.mock('@/shared/api', () => ({
  httpClient: { request: vi.fn() },
}))

describe('assetApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const mockAsset: AssetDto = {
    id: 'freezer-1',
    type: 'freezer',
    connections: [],
    metadata: {},
    createdAt: '2026-02-18T10:00:00Z',
    updatedAt: '2026-02-18T10:00:00Z',
  }

  describe('getAssets', () => {
    it('calls httpClient.request with /api/assets and returns result', async () => {
      vi.mocked(httpClient.request).mockResolvedValue([mockAsset])

      const result = await getAssets()

      expect(httpClient.request).toHaveBeenCalledWith('/api/assets')
      expect(result).toEqual([mockAsset])
    })
  })

  describe('getAssetById', () => {
    it('calls httpClient.request with /api/assets/:id', async () => {
      vi.mocked(httpClient.request).mockResolvedValue(mockAsset)

      const result = await getAssetById('freezer-1')

      expect(httpClient.request).toHaveBeenCalledWith('/api/assets/freezer-1')
      expect(result).toEqual(mockAsset)
    })
  })

  describe('createAsset', () => {
    it('POSTs to /api/assets with body', async () => {
      const body: CreateAssetRequest = {
        type: 'freezer',
        connections: ['c1'],
        metadata: {},
      }
      vi.mocked(httpClient.request).mockResolvedValue({ ...mockAsset, id: 'new-1' })

      const result = await createAsset(body)

      expect(httpClient.request).toHaveBeenCalledWith('/api/assets', {
        method: 'POST',
        body: JSON.stringify(body),
      })
      expect(result.id).toBe('new-1')
    })
  })

  describe('updateAsset', () => {
    it('PUTs to /api/assets/:id with body', async () => {
      const body: UpdateAssetRequest = {
        connections: ['c1', 'c2'],
      }
      vi.mocked(httpClient.request).mockResolvedValue({
        ...mockAsset,
        connections: ['c1', 'c2'],
      })

      await updateAsset('freezer-1', body)

      expect(httpClient.request).toHaveBeenCalledWith('/api/assets/freezer-1', {
        method: 'PUT',
        body: JSON.stringify(body),
      })
    })
  })
})
