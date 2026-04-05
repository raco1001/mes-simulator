import type { ExtraProperty } from '@/entities/asset'
import type {
  ObjectTypeSchemaDto,
  PropertyDefinition,
  SimulationBehavior,
} from '@/entities/object-type-schema'
import { normalizeExtraPropertiesList } from '@/shared/lib/extraPropertiesCore'

/** metadata 안의 예약 키 — extraProperties는 별도 state로 관리되므로 flat extra 목록에서 제외 */
export const EXTRA_PROPERTIES_KEY = 'extraProperties' as const

/** API/레거시에서 camelCase 외 키로 들어올 수 있음 (예: extraproperties) */
export function isReservedExtraPropertiesKey(key: string): boolean {
  return key.toLowerCase() === 'extraproperties'
}

export function stripReservedExtraPropertiesKeys(
  meta: Record<string, unknown>,
): Record<string, unknown> {
  const out = { ...meta }
  for (const k of Object.keys(out)) {
    if (isReservedExtraPropertiesKey(k)) delete out[k]
  }
  return out
}

export function getReservedExtraPropertiesRaw(
  meta: Record<string, unknown>,
): unknown {
  for (const k of Object.keys(meta)) {
    if (isReservedExtraPropertiesKey(k)) return meta[k]
  }
  return undefined
}

export function isEligibleProperty(p: PropertyDefinition): boolean {
  return (
    p.dataType === 'Number' &&
    (['Settable', 'Rate', 'Accumulator'] as SimulationBehavior[]).includes(p.simulationBehavior) &&
    p.mutability === 'Mutable'
  )
}

/** 관계 속성 매핑 후보: Number 타입만 (시뮬 동작 제한 없음). */
export function isNumberMappingProperty(p: PropertyDefinition): boolean {
  return p.dataType === 'Number'
}

/**
 * 관계 매핑 UI용: 스키마·extraProperties 중 Number 속성 전부.
 * (전파 시뮬 후보보다 넓게 — Power/Consume 등 Constant도 포함)
 */
export function mergeNumberMappingProperties(
  schema: ObjectTypeSchemaDto | null,
  assetMetadata: Record<string, unknown> | undefined,
): PropertyDefinition[] {
  const schemaProps = schema
    ? (schema.resolvedProperties ?? schema.ownProperties).filter(
        isNumberMappingProperty,
      )
    : []
  const extraDefs = parseExtraPropertiesFromMetadata(assetMetadata)
    .map(extraPropertyToPropertyDefinition)
    .filter(isNumberMappingProperty)

  const byKey = new Map<string, PropertyDefinition>()
  for (const p of schemaProps) {
    byKey.set(p.key, p)
  }
  for (const p of extraDefs) {
    byKey.set(p.key, p)
  }
  return [...byKey.values()]
}

/** metadata.extraProperties 배열 → ExtraProperty[] (훅과 동일 규칙). */
export function parseExtraPropertiesFromMetadata(
  meta: Record<string, unknown> | undefined,
): ExtraProperty[] {
  const raw = meta ? getReservedExtraPropertiesRaw(meta) : undefined
  if (!Array.isArray(raw)) return []
  return normalizeExtraPropertiesList(raw)
}

/** 관계 매핑 드롭다운용 — 스키마 PropertyDefinition과 동일 형태. */
export function extraPropertyToPropertyDefinition(
  ep: ExtraProperty,
): PropertyDefinition {
  return {
    key: ep.key,
    dataType: ep.dataType,
    unit: ep.unit,
    simulationBehavior: ep.simulationBehavior,
    mutability: ep.mutability,
    baseValue: ep.value,
    constraints: ep.constraints,
    required: false,
  }
}

/**
 * 관계 속성 매핑 후보: own/resolved 스키마 + 에셋 metadata.extraProperties.
 * 동일 key면 extra 정의가 우선(Phase 20과 동일).
 */
export function mergeEligibleMappingProperties(
  schema: ObjectTypeSchemaDto | null,
  assetMetadata: Record<string, unknown> | undefined,
): PropertyDefinition[] {
  const schemaProps = schema
    ? (schema.resolvedProperties ?? schema.ownProperties).filter(isEligibleProperty)
    : []
  const extraDefs = parseExtraPropertiesFromMetadata(assetMetadata)
    .map(extraPropertyToPropertyDefinition)
    .filter(isEligibleProperty)

  const byKey = new Map<string, PropertyDefinition>()
  for (const p of schemaProps) {
    byKey.set(p.key, p)
  }
  for (const p of extraDefs) {
    byKey.set(p.key, p)
  }
  return [...byKey.values()]
}

/** ObjectType 변경 시 스키마 기본값 주입 + 스키마에 없던 키는 유지 */
export function buildMetadataFromTypeSelection(
  objectType: string,
  schemas: ObjectTypeSchemaDto[],
  previousMeta: Record<string, unknown>,
): Record<string, unknown> {
  const schema = schemas.find((s) => s.objectType === objectType)
  const props = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const schemaKeys = new Set(props.map((p) => p.key))
  const injected: Record<string, unknown> = {}
  for (const p of props) {
    injected[p.key] = p.baseValue ?? ''
  }
  const preserved: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(previousMeta)) {
    if (!schemaKeys.has(k)) preserved[k] = v
  }
  return stripReservedExtraPropertiesKeys({ ...injected, ...preserved })
}

/** 패널 오픈 시 에셋 메타 + 스키마 기본값 병합 */
export function mergeAssetMetadataWithSchema(
  objectType: string,
  schemas: ObjectTypeSchemaDto[],
  assetMeta: Record<string, unknown>,
): Record<string, unknown> {
  const schema = schemas.find((s) => s.objectType === objectType)
  const props = schema?.resolvedProperties ?? schema?.ownProperties ?? []
  const out: Record<string, unknown> = { ...assetMeta }
  for (const p of props) {
    if (out[p.key] === undefined) out[p.key] = p.baseValue ?? ''
  }
  return stripReservedExtraPropertiesKeys(out)
}
