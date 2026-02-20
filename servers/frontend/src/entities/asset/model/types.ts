/**
 * Asset entity types (shared/api-schemas/assets.json 기반)
 */

export interface AssetDto {
  id: string
  type: string
  connections: string[]
  metadata: Record<string, unknown>
  createdAt: string
  updatedAt: string
}

/** Asset 생성 요청 (POST /api/assets) */
export interface CreateAssetRequest {
  type: string
  connections: string[]
  metadata: Record<string, unknown>
}

/** Asset 수정 요청 (PUT /api/assets/:id) */
export interface UpdateAssetRequest {
  type?: string
  connections: string[]
  metadata?: Record<string, unknown>
}
