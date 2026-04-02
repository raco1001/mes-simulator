import { httpClient } from '@/shared/api'
import type {
  CreateObjectTypeSchemaRequest,
  ObjectTypeSchemaDto,
  UpdateObjectTypeSchemaRequest,
} from '../model/types'

export async function getObjectTypeSchemas(): Promise<ObjectTypeSchemaDto[]> {
  return httpClient.request<ObjectTypeSchemaDto[]>('/api/object-type-schemas')
}

export async function getObjectTypeSchema(
  objectType: string,
): Promise<ObjectTypeSchemaDto> {
  return httpClient.request<ObjectTypeSchemaDto>(
    `/api/object-type-schemas/${encodeURIComponent(objectType)}`,
  )
}

export async function createObjectTypeSchema(
  request: CreateObjectTypeSchemaRequest,
): Promise<ObjectTypeSchemaDto> {
  return httpClient.request<ObjectTypeSchemaDto>('/api/object-type-schemas', {
    method: 'POST',
    body: JSON.stringify(request),
    headers: { 'Content-Type': 'application/json' },
  })
}

export async function updateObjectTypeSchema(
  objectType: string,
  request: UpdateObjectTypeSchemaRequest,
): Promise<ObjectTypeSchemaDto> {
  return httpClient.request<ObjectTypeSchemaDto>(
    `/api/object-type-schemas/${encodeURIComponent(objectType)}`,
    {
      method: 'PUT',
      body: JSON.stringify(request),
      headers: { 'Content-Type': 'application/json' },
    },
  )
}

export async function deleteObjectTypeSchema(objectType: string): Promise<void> {
  await httpClient.requestVoid(
    `/api/object-type-schemas/${encodeURIComponent(objectType)}`,
    { method: 'DELETE' },
  )
}
