import { httpClient } from '@/shared/api'
import type {
  CreateRelationshipRequest,
  RelationshipDto,
  UpdateRelationshipRequest,
} from '../model/types'

export async function getRelationships(): Promise<RelationshipDto[]> {
  const list = await httpClient.request<RelationshipDto[]>('/api/relationships')
  return list
}

export async function getRelationshipById(id: string): Promise<RelationshipDto> {
  return httpClient.request<RelationshipDto>(`/api/relationships/${encodeURIComponent(id)}`)
}

export async function createRelationship(
  body: CreateRelationshipRequest,
): Promise<RelationshipDto> {
  return httpClient.request<RelationshipDto>('/api/relationships', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export async function updateRelationship(
  id: string,
  body: UpdateRelationshipRequest,
): Promise<RelationshipDto> {
  return httpClient.request<RelationshipDto>(`/api/relationships/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

export async function deleteRelationship(id: string): Promise<void> {
  const url = `${httpClient.baseUrl}/api/relationships/${encodeURIComponent(id)}`
  const response = await fetch(url, { method: 'DELETE' })
  if (!response.ok) {
    if (response.status === 404) throw new Error('Resource not found')
    throw new Error(`API request failed: ${response.statusText}`)
  }
  // 204 No Content has no body; avoid response.json()
}
