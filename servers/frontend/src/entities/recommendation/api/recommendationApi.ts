import { httpClient } from '@/shared/api'
import type { RecommendationDto, RecommendationStatus } from '../model/types'

export async function getRecommendations(params?: {
  status?: RecommendationStatus
  severity?: RecommendationDto['severity']
}): Promise<RecommendationDto[]> {
  const qs = new URLSearchParams()
  if (params?.status) qs.set('status', params.status)
  if (params?.severity) qs.set('severity', params.severity)
  const suffix = qs.size > 0 ? `?${qs.toString()}` : ''
  return httpClient.request<RecommendationDto[]>(`/api/recommendations${suffix}`)
}

export async function getRecommendationById(
  recommendationId: string,
): Promise<RecommendationDto> {
  return httpClient.request<RecommendationDto>(
    `/api/recommendations/${encodeURIComponent(recommendationId)}`,
  )
}

export async function updateRecommendationStatus(
  recommendationId: string,
  status: RecommendationStatus,
): Promise<RecommendationDto> {
  return httpClient.request<RecommendationDto>(
    `/api/recommendations/${encodeURIComponent(recommendationId)}`,
    {
      method: 'PATCH',
      body: JSON.stringify({ status }),
      headers: { 'Content-Type': 'application/json' },
    },
  )
}

export async function applyRecommendation(
  recommendationId: string,
): Promise<{ success: boolean; runId: string; recommendation: RecommendationDto }> {
  return httpClient.request<{ success: boolean; runId: string; recommendation: RecommendationDto }>(
    `/api/recommendations/${encodeURIComponent(recommendationId)}/apply`,
    { method: 'POST' },
  )
}
