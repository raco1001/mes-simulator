import type {
  ObjectTypeSchemaDto,
  PropertyDefinition,
  SimulationBehavior,
} from '@/entities/object-type-schema'

/** metadata 안의 예약 키 — extraProperties는 별도 state로 관리되므로 flat extra 목록에서 제외 */
export const EXTRA_PROPERTIES_KEY = 'extraProperties' as const

export function isEligibleProperty(p: PropertyDefinition): boolean {
  return (
    p.dataType === 'Number' &&
    (['Settable', 'Rate', 'Accumulator'] as SimulationBehavior[]).includes(p.simulationBehavior) &&
    p.mutability === 'Mutable'
  )
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
  return { ...injected, ...preserved }
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
  return out
}
