import { httpClient } from '@/shared/api'
import type { AssetDto, CreateAssetRequest, UpdateAssetRequest } from '../model/types'

export async function getAssets(): Promise<AssetDto[]> {
  return httpClient.request<AssetDto[]>('/api/assets')
}

export async function getAssetById(id: string): Promise<AssetDto> {
  return httpClient.request<AssetDto>(`/api/assets/${id}`)
}

export async function createAsset(body: CreateAssetRequest): Promise<AssetDto> {
  return httpClient.request<AssetDto>('/api/assets', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export async function updateAsset(
  id: string,
  body: UpdateAssetRequest,
): Promise<AssetDto> {
  return httpClient.request<AssetDto>(`/api/assets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}
