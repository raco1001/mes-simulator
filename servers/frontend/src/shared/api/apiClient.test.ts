import { describe, it, expect, vi, beforeEach } from 'vitest'
import { apiClient } from './apiClient'
import type { AssetDto, StateDto } from './types'

// Mock fetch globally
const mockFetch = vi.fn()
// @ts-expect-error - Mocking global fetch for testing
global.fetch = mockFetch as unknown as typeof fetch

describe('ApiClient', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getAssets', () => {
    it('should fetch all assets', async () => {
      const mockAssets: AssetDto[] = [
        {
          id: 'freezer-1',
          type: 'freezer',
          connections: [],
          metadata: {},
          createdAt: '2026-02-18T10:00:00Z',
          updatedAt: '2026-02-18T10:00:00Z',
        },
      ]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockAssets,
      } as Response)

      const result = await apiClient.getAssets()

      expect(mockFetch).toHaveBeenCalledWith('http://localhost:5000/api/assets', {
        headers: { 'Content-Type': 'application/json' },
      })
      expect(result).toEqual(mockAssets)
    })

    it('should throw error on 404', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
      } as Response)

      await expect(apiClient.getAssets()).rejects.toThrow('Resource not found')
    })
  })

  describe('getAssetById', () => {
    it('should fetch asset by id', async () => {
      const mockAsset: AssetDto = {
        id: 'freezer-1',
        type: 'freezer',
        connections: [],
        metadata: {},
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockAsset,
      } as Response)

      const result = await apiClient.getAssetById('freezer-1')

      expect(mockFetch).toHaveBeenCalledWith('http://localhost:5000/api/assets/freezer-1', {
        headers: { 'Content-Type': 'application/json' },
      })
      expect(result).toEqual(mockAsset)
    })
  })

  describe('getStates', () => {
    it('should fetch all states', async () => {
      const mockStates: StateDto[] = [
        {
          assetId: 'freezer-1',
          currentTemp: -5.0,
          currentPower: 120.0,
          status: 'normal',
          lastEventType: 'asset.health.updated',
          updatedAt: '2026-02-18T10:00:00Z',
          metadata: {},
        },
      ]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockStates,
      } as Response)

      const result = await apiClient.getStates()

      expect(mockFetch).toHaveBeenCalledWith('http://localhost:5000/api/states', {
        headers: { 'Content-Type': 'application/json' },
      })
      expect(result).toEqual(mockStates)
    })
  })

  describe('getStateByAssetId', () => {
    it('should fetch state by asset id', async () => {
      const mockState: StateDto = {
        assetId: 'freezer-1',
        currentTemp: -5.0,
        currentPower: 120.0,
        status: 'normal',
        lastEventType: 'asset.health.updated',
        updatedAt: '2026-02-18T10:00:00Z',
        metadata: {},
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockState,
      } as Response)

      const result = await apiClient.getStateByAssetId('freezer-1')

      expect(mockFetch).toHaveBeenCalledWith('http://localhost:5000/api/states/freezer-1', {
        headers: { 'Content-Type': 'application/json' },
      })
      expect(result).toEqual(mockState)
    })
  })

  describe('createAsset', () => {
    it('should POST create asset and return AssetDto', async () => {
      const body = { type: 'freezer', connections: ['conveyor-1'], metadata: { capacity: 1000 } }
      const mockAsset: AssetDto = {
        id: 'freezer-new',
        type: 'freezer',
        connections: ['conveyor-1'],
        metadata: { capacity: 1000 },
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T10:00:00Z',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockAsset,
      } as Response)

      const result = await apiClient.createAsset(body)

      expect(mockFetch).toHaveBeenCalledWith('http://localhost:5000/api/assets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      expect(result).toEqual(mockAsset)
    })
  })

  describe('updateAsset', () => {
    it('should PUT update asset and return AssetDto', async () => {
      const id = 'freezer-1'
      const body = { type: 'freezer', connections: ['conveyor-1', 'conveyor-2'] }
      const mockAsset: AssetDto = {
        id: 'freezer-1',
        type: 'freezer',
        connections: ['conveyor-1', 'conveyor-2'],
        metadata: {},
        createdAt: '2026-02-18T10:00:00Z',
        updatedAt: '2026-02-18T11:00:00Z',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockAsset,
      } as Response)

      const result = await apiClient.updateAsset(id, body)

      expect(mockFetch).toHaveBeenCalledWith(`http://localhost:5000/api/assets/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      expect(result).toEqual(mockAsset)
    })
  })
})
