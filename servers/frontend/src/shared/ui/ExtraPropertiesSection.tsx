import type { ExtraProperty } from '@/entities/asset'
import type { DataType } from '@/entities/object-type-schema'
import { UnitSelect } from '@/shared/ui/UnitSelect'

const DATA_TYPES = [
  'Number',
  'String',
  'Boolean',
  'DateTime',
  'Array',
  'Object',
] as const satisfies readonly DataType[]

export function ExtraPropertiesSection({
  extraProperties,
  onAdd,
  onUpdate,
  onRemove,
}: {
  extraProperties: ExtraProperty[]
  onAdd: () => void
  onUpdate: (index: number, patch: Partial<ExtraProperty>) => void
  onRemove: (index: number) => void
}) {
  return (
    <div className="assets-canvas-page__meta-section">
      <span>확장 속성 (extraProperties)</span>
      {extraProperties.map((p, i) => (
        <div
          key={i}
          className="assets-canvas-page__meta-row assets-canvas-page__meta-row--extra"
        >
          <input
            placeholder="key"
            value={p.key}
            onChange={(e) => onUpdate(i, { key: e.target.value })}
            aria-label={`extra-prop-key-${i}`}
          />
          <div className="assets-canvas-page__meta-row-selects">
            <select
              value={p.dataType}
              onChange={(e) =>
                onUpdate(i, { dataType: e.target.value as DataType })
              }
              aria-label={`extra-prop-datatype-${i}`}
            >
              {DATA_TYPES.map((dt) => (
                <option key={dt} value={dt}>
                  {dt}
                </option>
              ))}
            </select>
            {p.dataType === 'Number' ? (
              <UnitSelect
                compact
                value={p.unit}
                onChange={(unit) => onUpdate(i, { unit: unit || undefined })}
              />
            ) : null}
          </div>
          <input
            placeholder="value"
            value={String(p.value ?? '')}
            onChange={(e) => onUpdate(i, { value: e.target.value })}
            aria-label={`extra-prop-value-${i}`}
          />
          <button
            type="button"
            className="assets-canvas-page__meta-row-delete"
            onClick={() => onRemove(i)}
          >
            삭제
          </button>
        </div>
      ))}
      <button
        type="button"
        className="assets-canvas-page__meta-section-add-btn"
        onClick={onAdd}
      >
        + 속성 추가
      </button>
    </div>
  )
}

function formatFlatMetadataValue(value: unknown): string {
  if (value === null || value === undefined) return ''
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean')
    return String(value)
  try {
    return JSON.stringify(value)
  } catch {
    return String(value)
  }
}

export function FlatExtraMetadataSection({
  extraKeys,
  metadata,
  onSetValue,
  onRemoveKey,
  onAddRow,
  addButtonLabel,
}: {
  extraKeys: string[]
  metadata: Record<string, unknown>
  onSetValue: (key: string, raw: string) => void
  onRemoveKey: (key: string) => void
  onAddRow: () => void
  addButtonLabel: string
}) {
  return (
    <div className="assets-canvas-page__meta-section">
      <span>추가 메타데이터 (스키마 외 키)</span>
      {extraKeys.map((key) => (
        <div
          key={key}
          className="assets-canvas-page__meta-row assets-canvas-page__meta-row--extra"
        >
          <input
            placeholder="key"
            value={key}
            readOnly
            aria-label="extra-key"
          />
          <input
            placeholder="value"
            value={formatFlatMetadataValue(metadata[key])}
            onChange={(e) => onSetValue(key, e.target.value)}
          />
          <button
            type="button"
            className="assets-canvas-page__meta-row-delete"
            onClick={() => onRemoveKey(key)}
          >
            삭제
          </button>
        </div>
      ))}
      <button
        type="button"
        className="assets-canvas-page__meta-section-add-btn"
        onClick={onAddRow}
      >
        {addButtonLabel}
      </button>
    </div>
  )
}
