import { beforeEach, describe, expect, it, vi } from 'vitest'
import { httpClient } from '@/shared/api'
import {
  getRecommendationById,
  getRecommendations,
  updateRecommendationStatus,
} from './recommendationApi'

vi.mock('@/shared/api', () => ({
  httpClient: { request: vi.fn() },
}))

describe('recommendationApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('getRecommendations calls list endpoint', async () => {
    vi.mocked(httpClient.request).mockResolvedValue([])
    await getRecommendations({ status: 'pending', severity: 'warning' })
    expect(httpClient.request).toHaveBeenCalledWith(
      '/api/recommendations?status=pending&severity=warning',
    )
  })

  it('getRecommendationById calls detail endpoint', async () => {
    vi.mocked(httpClient.request).mockResolvedValue({ recommendationId: 'r1' })
    await getRecommendationById('r1')
    expect(httpClient.request).toHaveBeenCalledWith('/api/recommendations/r1')
  })

  it('updateRecommendationStatus calls patch endpoint', async () => {
    vi.mocked(httpClient.request).mockResolvedValue({ recommendationId: 'r1' })
    await updateRecommendationStatus('r1', 'approved')
    expect(httpClient.request).toHaveBeenCalledWith('/api/recommendations/r1', {
      method: 'PATCH',
      body: JSON.stringify({ status: 'approved' }),
      headers: { 'Content-Type': 'application/json' },
    })
  })
})
