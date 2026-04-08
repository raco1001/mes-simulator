import {
  getMetadataAssetName,
  isHiddenFromFlatMetadataKeys,
} from '@/shared/lib/canvasMetadata'

/** 캔버스 노드 제목: 표시 이름 우선, 없으면 ObjectType 코드 */
export function getAssetDisplayTitle(asset: {
  type: string
  metadata?: Record<string, unknown>
}): string {
  const name = getMetadataAssetName(asset.metadata ?? {}).trim()
  return name || asset.type
}

/** 메타데이터 값 한 줄 미리보기 (배열·객체는 길이·키 요약) */
export function formatMetadataValuePreview(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean')
    return String(value)
  if (Array.isArray(value)) {
    if (value.length === 0) return '(0)'
    const keys = value
      .map((item) => {
        if (item !== null && typeof item === 'object' && !Array.isArray(item)) {
          const k = (item as Record<string, unknown>).key
          return typeof k === 'string' ? k : null
        }
        return null
      })
      .filter((k): k is string => k != null && k.length > 0)
    if (keys.length > 0) {
      const preview = keys.slice(0, 3).join(', ')
      return keys.length > 3 ? `${preview}, …` : preview
    }
    return `(${value.length} items)`
  }
  if (typeof value === 'object') {
    const keys = Object.keys(value as object)
    if (keys.length === 0) return '{}'
    const head = keys.slice(0, 2).join(', ')
    return keys.length > 2 ? `{${head}, …}` : `{${head}}`
  }
  return String(value)
}

/** 노드 하단 등: well-known 키 제외한 메타 요약 */
export function formatAssetMetadataSummary(
  meta: Record<string, unknown> | undefined,
  maxEntries = 2,
): string {
  if (!meta || Object.keys(meta).length === 0) return '-'
  const entries = Object.entries(meta).filter(
    ([k]) => !isHiddenFromFlatMetadataKeys(k),
  )
  if (entries.length === 0) return '-'
  return entries
    .slice(0, maxEntries)
    .map(([k, v]) => `${k}: ${formatMetadataValuePreview(v)}`)
    .join(', ')
}
