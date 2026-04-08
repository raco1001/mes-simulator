/**
 * Shared Derived property constraint helpers (ObjectType schema, extraProperties, asset UI).
 */

export const DERIVED_OPERATIONS = ['sum', 'avg', 'min', 'max'] as const
export type DerivedOperation = (typeof DERIVED_OPERATIONS)[number]

export function parseDependsOnCsv(csv: string): string[] {
  return csv
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
}

export function formatDependsOnAsCsv(dependsOn: unknown): string {
  if (!Array.isArray(dependsOn)) return ''
  return dependsOn
    .map((v) => String(v).trim())
    .filter((v) => v.length > 0)
    .join(', ')
}

export function readDerivedDependsOn(p: {
  constraints?: Record<string, unknown>
}): string {
  return formatDependsOnAsCsv(p.constraints?.dependsOn)
}

export function readDerivedOperation(p: {
  constraints?: Record<string, unknown>
}): string {
  const raw = p.constraints?.operation
  if (typeof raw !== 'string') return 'sum'
  const op = raw.trim().toLowerCase()
  return DERIVED_OPERATIONS.includes(op as DerivedOperation) ? op : 'sum'
}
