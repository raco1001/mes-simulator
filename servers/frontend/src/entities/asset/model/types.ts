/**
 * Asset entity types (shared/api-schemas/assets.json 기반)
 */

import type {
  DataType,
  Mutability,
  SimulationBehavior,
} from '@/entities/object-type-schema'

/** metadata.extraProperties 배열 항목 — 시뮬 엔진이 ownProperties와 동일 경로로 처리 */
export interface ExtraProperty {
  key: string
  dataType: DataType
  unit?: string
  value: unknown
  simulationBehavior: SimulationBehavior
  mutability: Mutability
  constraints?: Record<string, unknown>
}

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
