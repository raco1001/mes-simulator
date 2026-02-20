import { httpClient } from '@/shared/api'
import type { StateDto } from '../model/types'

export async function getStates(): Promise<StateDto[]> {
  return httpClient.request<StateDto[]>('/api/states')
}

export async function getStateByAssetId(assetId: string): Promise<StateDto> {
  return httpClient.request<StateDto>(`/api/states/${assetId}`)
}
