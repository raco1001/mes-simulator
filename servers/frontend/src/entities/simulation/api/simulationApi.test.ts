import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from '@/shared/api'
import {
  appendSimulationOverride,
  getRun,
  getRunningSimulationRuns,
} from './simulationApi'
import type { AppendSimulationOverrideRequestDto, SimulationRunDetailDto } from '../model/types'

vi.mock('@/shared/api', () => ({
  httpClient: { request: vi.fn(), requestVoid: vi.fn() },
}))

describe('simulationApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('getRun', () => {
    it('GETs /api/simulation/runs/:runId', async () => {
      const detail: SimulationRunDetailDto = {
        id: 'run-1',
        status: 'Running',
        startedAt: '2026-01-01T00:00:00Z',
        triggerAssetId: 'a1',
        tickIndex: 0,
        overrides: [],
      }
      vi.mocked(httpClient.request).mockResolvedValue(detail)

      const result = await getRun('run-1')

      expect(httpClient.request).toHaveBeenCalledWith('/api/simulation/runs/run-1')
      expect(result).toEqual(detail)
    })

    it('encodes runId in path', async () => {
      vi.mocked(httpClient.request).mockResolvedValue({})

      await getRun('a/b')

      expect(httpClient.request).toHaveBeenCalledWith('/api/simulation/runs/a%2Fb')
    })
  })

  describe('getRunningSimulationRuns', () => {
    it('GETs /api/simulation/running', async () => {
      vi.mocked(httpClient.request).mockResolvedValue([
        {
          id: 'r1',
          status: 'Running',
          startedAt: '',
          triggerAssetId: 'a1',
          tickIndex: 0,
          overrides: [],
        },
      ])

      const runs = await getRunningSimulationRuns()

      expect(httpClient.request).toHaveBeenCalledWith('/api/simulation/running')
      expect(runs).toHaveLength(1)
      expect(runs[0].id).toBe('r1')
    })

    it('returns empty array when body is undefined', async () => {
      vi.mocked(httpClient.request).mockResolvedValue(undefined)

      const runs = await getRunningSimulationRuns()

      expect(runs).toEqual([])
    })
  })

  describe('appendSimulationOverride', () => {
    it('POSTs overrides body via requestVoid', async () => {
      vi.mocked(httpClient.requestVoid).mockResolvedValue(undefined)

      const body: AppendSimulationOverrideRequestDto = {
        assetId: 'a1',
        propertyKey: 'temp',
        value: 5,
        fromTick: 1,
      }
      await appendSimulationOverride('run-2', body)

      expect(httpClient.requestVoid).toHaveBeenCalledWith(
        '/api/simulation/runs/run-2/overrides',
        {
          method: 'POST',
          body: JSON.stringify(body),
          headers: { 'Content-Type': 'application/json' },
        },
      )
    })
  })
})
