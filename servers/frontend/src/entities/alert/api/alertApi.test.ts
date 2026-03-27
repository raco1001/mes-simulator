import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from '@/shared/api'
import { getAlerts } from './alertApi'

vi.mock('@/shared/api', () => ({
  httpClient: { request: vi.fn() },
}))

describe('alertApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls /api/alerts with default limit', async () => {
    vi.mocked(httpClient.request).mockResolvedValue([])

    await getAlerts()

    expect(httpClient.request).toHaveBeenCalledWith('/api/alerts?limit=50')
  })

  it('calls /api/alerts with provided limit', async () => {
    vi.mocked(httpClient.request).mockResolvedValue([])

    await getAlerts(10)

    expect(httpClient.request).toHaveBeenCalledWith('/api/alerts?limit=10')
  })
})
