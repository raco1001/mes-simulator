import { httpClient } from '@/shared/api'
import type { AlertDto } from '../model/types'

export async function getAlerts(limit = 50): Promise<AlertDto[]> {
  return httpClient.request<AlertDto[]>(`/api/alerts?limit=${limit}`)
}
