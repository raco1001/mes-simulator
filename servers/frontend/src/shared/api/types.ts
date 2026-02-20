/**
 * Backend API 타입 정의
 * shared/api-schemas/assets.json 기반
 */

export interface AssetDto {
  id: string
  type: string
  connections: string[]
  metadata: Record<string, unknown>
  createdAt: string
  updatedAt: string
}

export interface StateDto {
  assetId: string
  currentTemp: number | null
  currentPower: number | null
  status: 'normal' | 'warning' | 'error'
  lastEventType: string | null
  updatedAt: string
  metadata: Record<string, unknown>
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
