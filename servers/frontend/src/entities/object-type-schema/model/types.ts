export type DataType =
  | 'Number'
  | 'String'
  | 'Boolean'
  | 'DateTime'
  | 'Array'
  | 'Object'

export type SimulationBehavior =
  | 'Constant'
  | 'Settable'
  | 'Rate'
  | 'Accumulator'
  | 'Derived'

export type Mutability = 'Immutable' | 'Mutable'

export interface PropertyDefinition {
  key: string
  dataType: DataType
  unit?: string
  simulationBehavior: SimulationBehavior
  mutability: Mutability
  baseValue?: unknown
  constraints?: Record<string, unknown>
  required: boolean
}

export interface ObjectTypeTraits {
  persistence: 'Permanent' | 'Durable' | 'Transient'
  dynamism: 'Static' | 'Dynamic' | 'Reactive'
  cardinality: 'Singular' | 'Enumerable' | 'Streaming'
}

export interface Classification {
  taxonomy: string
  value: string
}

export interface AllowedLink {
  linkType: string
  direction: 'inbound' | 'outbound' | 'bidirectional'
  targetTraits?: Partial<ObjectTypeTraits>
}

export interface ObjectTypeSchemaDto {
  schemaVersion: string
  objectType: string
  displayName: string
  abstractSchema?: boolean
  extends?: string
  traits: ObjectTypeTraits
  classifications: Classification[]
  /** Properties defined directly on this schema (excluding inherited). */
  ownProperties: PropertyDefinition[]
  /** Runtime-merged properties: parent.ownProperties + this.ownProperties. Populated by backend. */
  resolvedProperties?: PropertyDefinition[]
  allowedLinks: AllowedLink[]
  createdAt?: string
  updatedAt?: string
}

export type CreateObjectTypeSchemaRequest = Omit<
  ObjectTypeSchemaDto,
  'createdAt' | 'updatedAt' | 'resolvedProperties'
>

export type UpdateObjectTypeSchemaRequest = Partial<
  Omit<ObjectTypeSchemaDto, 'objectType' | 'createdAt' | 'updatedAt'>
>
