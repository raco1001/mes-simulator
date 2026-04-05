import type { PropertyMapping } from '@/entities/relationship'
import type { PropertyDefinition } from '@/entities/object-type-schema'

export type LinkDirection = 'Directed' | 'Bidirectional' | 'Hierarchical'
export type LinkTemporality = 'Permanent' | 'Durable' | 'EventDriven'

export interface LinkConstraint {
  requiredTraits?: Record<string, string>
  allowedObjectTypes?: string[]
}

/** LinkType 온톨로지: 허용되는 (from→to) 속성 쌍 화이트리스트. */
export interface PropertyMappingPairHint {
  fromPropertyKey: string
  toPropertyKey: string
}

export interface LinkTypeSchemaDto {
  schemaVersion: string
  linkType: string
  displayName: string
  direction: LinkDirection
  temporality: LinkTemporality
  fromConstraint: LinkConstraint | null
  toConstraint: LinkConstraint | null
  properties: PropertyDefinition[]
  /** 관계 생성 시 mappings가 비어 있으면 초기 매핑으로 사용 */
  defaultPropertyMappings?: PropertyMapping[]
  /** 비어 있지 않으면 각 매핑이 여기 나열한 쌍과 일치해야 함 */
  allowedPropertyMappingPairs?: PropertyMappingPairHint[]
  createdAt?: string
  updatedAt?: string
}
