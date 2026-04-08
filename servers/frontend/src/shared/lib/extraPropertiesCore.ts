/**
 * Shared normalization for metadata.extraProperties array items.
 * Kept separate from canvasMetadata to avoid circular imports.
 */
import type { ExtraProperty } from '@/entities/asset'
import type { DataType, Mutability, SimulationBehavior } from '@/entities/object-type-schema'

const DEFAULT_SIM_BEHAVIOR: SimulationBehavior = 'Settable'
const DEFAULT_MUTABILITY: Mutability = 'Mutable'

const VALID_DATA_TYPES: DataType[] = [
  'Number',
  'String',
  'Boolean',
  'DateTime',
  'Array',
  'Object',
]
const VALID_SIM: SimulationBehavior[] = [
  'Constant',
  'Settable',
  'Rate',
  'Accumulator',
  'Derived',
]
const VALID_MUT: Mutability[] = ['Immutable', 'Mutable']

function coerceDataType(v: unknown): DataType {
  return VALID_DATA_TYPES.includes(v as DataType) ? (v as DataType) : 'String'
}

/** 레거시 Mongo 키(dataType → datatype 등)에서 읽기 */
function pickDataTypeField(o: Record<string, unknown>): unknown {
  return o.dataType ?? o.datatype
}

function pickSimulationBehaviorField(o: Record<string, unknown>): unknown {
  return o.simulationBehavior ?? o.simulationbehavior
}

/**
 * 저장 직전: UI는 문자열 입력이 많아 API/ BSON이 dataType에 맞는 JSON 타입을 보내도록 정규화.
 */
export function coerceValueForDataType(
  dataType: DataType,
  value: unknown,
): unknown {
  switch (dataType) {
    case 'Number': {
      if (typeof value === 'number' && Number.isFinite(value)) return value
      if (value === null || value === undefined || value === '') return 0
      if (typeof value === 'string') {
        const n = Number(value.trim())
        return Number.isFinite(n) ? n : 0
      }
      return 0
    }
    case 'Boolean': {
      if (typeof value === 'boolean') return value
      if (typeof value === 'string') {
        const s = value.trim().toLowerCase()
        if (s === 'true') return true
        if (s === 'false') return false
      }
      return Boolean(value)
    }
    case 'DateTime':
      if (value === null || value === undefined) return ''
      return typeof value === 'string' ? value : String(value)
    case 'Array': {
      if (Array.isArray(value)) return value
      if (typeof value === 'string') {
        const t = value.trim()
        if (!t) return []
        try {
          const parsed = JSON.parse(t) as unknown
          return Array.isArray(parsed) ? parsed : []
        } catch {
          return []
        }
      }
      return []
    }
    case 'Object': {
      if (value !== null && typeof value === 'object' && !Array.isArray(value))
        return value
      if (typeof value === 'string') {
        const t = value.trim()
        if (!t) return {}
        try {
          const parsed = JSON.parse(t) as unknown
          return parsed !== null &&
            typeof parsed === 'object' &&
            !Array.isArray(parsed)
            ? parsed
            : {}
        } catch {
          return {}
        }
      }
      return {}
    }
    case 'String':
    default:
      if (value === null || value === undefined) return ''
      return typeof value === 'string' ? value : String(value)
  }
}

export function sanitizeExtraPropertyForSave(p: ExtraProperty): ExtraProperty {
  return {
    ...p,
    value: coerceValueForDataType(p.dataType, p.value),
  }
}

function coerceSimulationBehavior(v: unknown): SimulationBehavior {
  return VALID_SIM.includes(v as SimulationBehavior)
    ? (v as SimulationBehavior)
    : DEFAULT_SIM_BEHAVIOR
}

function coerceMutability(v: unknown): Mutability {
  return VALID_MUT.includes(v as Mutability)
    ? (v as Mutability)
    : DEFAULT_MUTABILITY
}

/** 레거시 항목에 simulationBehavior / mutability 없을 때 기본값 부여 */
export function normalizeExtraProperty(raw: unknown): ExtraProperty | null {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null
  const o = raw as Record<string, unknown>
  const key = typeof o.key === 'string' ? o.key : ''
  const constraints =
    o.constraints &&
    typeof o.constraints === 'object' &&
    !Array.isArray(o.constraints)
      ? (o.constraints as Record<string, unknown>)
      : undefined
  return {
    key,
    dataType: coerceDataType(pickDataTypeField(o)),
    unit: typeof o.unit === 'string' ? o.unit : undefined,
    value: o.value,
    simulationBehavior: coerceSimulationBehavior(pickSimulationBehaviorField(o)),
    mutability: coerceMutability(o.mutability),
    constraints,
  }
}

export function normalizeExtraPropertiesList(items: unknown[]): ExtraProperty[] {
  const out: ExtraProperty[] = []
  for (const item of items) {
    const n = normalizeExtraProperty(item)
    if (n) out.push(n)
  }
  return out
}
