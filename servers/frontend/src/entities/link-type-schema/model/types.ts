import type { PropertyDefinition } from '@/entities/object-type-schema'

export type LinkDirection = 'Directed' | 'Bidirectional' | 'Hierarchical'
export type LinkTemporality = 'Permanent' | 'Durable' | 'EventDriven'

export interface LinkConstraint {
  requiredTraits?: Record<string, string>
  allowedObjectTypes?: string[]
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
  createdAt?: string
  updatedAt?: string
}
