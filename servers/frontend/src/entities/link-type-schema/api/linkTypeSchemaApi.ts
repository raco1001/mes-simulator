import { httpClient } from '@/shared/api'
import type { LinkTypeSchemaDto } from '../model/types'

export async function getLinkTypeSchemas(): Promise<LinkTypeSchemaDto[]> {
  return httpClient.request<LinkTypeSchemaDto[]>('/api/link-type-schemas')
}

export async function getLinkTypeSchema(
  linkType: string,
): Promise<LinkTypeSchemaDto> {
  return httpClient.request<LinkTypeSchemaDto>(
    `/api/link-type-schemas/${encodeURIComponent(linkType)}`,
  )
}
