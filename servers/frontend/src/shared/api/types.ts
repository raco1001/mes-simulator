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
