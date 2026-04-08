import { describe, it, expect } from 'vitest'
import { isTriggerCandidateAsset } from './RunSimulationOnPanel'
import type { AssetDto } from '@/entities/asset'
import type { ObjectTypeSchemaDto } from '@/entities/object-type-schema'

const baseTraits = {
  persistence: 'Durable' as const,
  dynamism: 'Dynamic' as const,
  cardinality: 'Singular' as const,
}

describe('isTriggerCandidateAsset', () => {
  it('returns false when no eligible mapping properties', () => {
    const asset: AssetDto = {
      id: 'a1',
      type: 'T',
      connections: [],
      metadata: {},
      createdAt: '',
      updatedAt: '',
    }
    const schemas: ObjectTypeSchemaDto[] = [
      {
        schemaVersion: 'v1',
        objectType: 'T',
        displayName: 'T',
        traits: baseTraits,
        classifications: [],
        ownProperties: [],
        resolvedProperties: [],
        allowedLinks: [],
      },
    ]
    expect(isTriggerCandidateAsset(asset, schemas)).toBe(false)
  })

  it('returns true when schema has Settable Number Mutable property', () => {
    const asset: AssetDto = {
      id: 'a1',
      type: 'T',
      connections: [],
      metadata: {},
      createdAt: '',
      updatedAt: '',
    }
    const powerProp = {
      key: 'power',
      dataType: 'Number' as const,
      simulationBehavior: 'Settable' as const,
      mutability: 'Mutable' as const,
      baseValue: 0,
      constraints: {},
      required: false,
    }
    const schemas: ObjectTypeSchemaDto[] = [
      {
        schemaVersion: 'v1',
        objectType: 'T',
        displayName: 'T',
        traits: baseTraits,
        classifications: [],
        ownProperties: [powerProp],
        resolvedProperties: [powerProp],
        allowedLinks: [],
      },
    ]
    expect(isTriggerCandidateAsset(asset, schemas)).toBe(true)
  })
})
