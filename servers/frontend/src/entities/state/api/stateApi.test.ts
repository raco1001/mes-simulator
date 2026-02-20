import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from '@/shared/api'
import { getStates, getStateByAssetId } from './stateApi'
import type { StateDto } from '../model/types'

vi.mock('@/shared/api', () => ({
  httpClient: { request: vi.fn() },
}))

describe('stateApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const mockState: StateDto = {
    assetId: 'freezer-1',
    currentTemp: -5,
    currentPower: 120,
    status: 'normal',
    lastEventType: null,
    updatedAt: '2026-02-18T10:00:00Z',
    metadata: {},
  }

  describe('getStates', () => {
    it('calls httpClient.request with /api/states and returns result', async () => {
      vi.mocked(httpClient.request).mockResolvedValue([mockState])

      const result = await getStates()

      expect(httpClient.request).toHaveBeenCalledWith('/api/states')
      expect(result).toEqual([mockState])
    })
  })

  describe('getStateByAssetId', () => {
    it('calls httpClient.request with /api/states/:assetId', async () => {
      vi.mocked(httpClient.request).mockResolvedValue(mockState)

      const result = await getStateByAssetId('freezer-1')

      expect(httpClient.request).toHaveBeenCalledWith('/api/states/freezer-1')
      expect(result).toEqual(mockState)
    })
  })
})
