import type { PropertyDefinition } from '@/entities/object-type-schema'
import {
  readDerivedDependsOn,
  readDerivedOperation,
} from '@/shared/lib/derivedPropertyConstraints'

export function SchemaPropertyRows({
  schemaProps,
  metadata,
  setMetaValue,
  ariaLabelPrefix = 'schema',
}: {
  schemaProps: PropertyDefinition[]
  metadata: Record<string, unknown>
  setMetaValue: (key: string, raw: string) => void
  /** e.g. `schema` → labels like `schema-foo-key`; omit for `foo-key` (에셋 편집과 동일) */
  ariaLabelPrefix?: string
}) {
  if (schemaProps.length === 0) return null

  return (
    <div className="assets-canvas-page__meta-section">
      <span>스키마 속성</span>
      {schemaProps.map((p) => {
        const isDerived = p.simulationBehavior === 'Derived'
        const valueReadOnly = p.mutability === 'Immutable' || isDerived
        const idBase = ariaLabelPrefix ? `${ariaLabelPrefix}-${p.key}` : p.key
        return (
          <div key={p.key}>
            <div className="assets-canvas-page__meta-row">
              <input value={p.key} readOnly aria-label={`${idBase}-key`} />
              <input
                value={String(metadata[p.key] ?? '')}
                readOnly={valueReadOnly}
                onChange={
                  valueReadOnly
                    ? undefined
                    : (e) => setMetaValue(p.key, e.target.value)
                }
                aria-label={`${idBase}-value`}
                title={
                  isDerived
                    ? '시뮬레이션에서 dependsOn에 나열한 속성 값으로 계산됩니다.'
                    : undefined
                }
              />
              <span className="assets-canvas-page__prop-badge">
                {p.dataType} / {p.simulationBehavior} / {p.mutability}
              </span>
            </div>
            {isDerived ? (
              <div
                className="assets-canvas-page__schema-derived-readonly"
                aria-label={`${idBase}-derived-rules`}
              >
                <span>dependsOn:</span>{' '}
                <code>{readDerivedDependsOn(p) || '—'}</code>
                <span> · operation: </span>
                <code>{readDerivedOperation(p)}</code>
                <span className="assets-canvas-page__schema-derived-note">
                  (ObjectType 스키마에서 변경)
                </span>
              </div>
            ) : null}
          </div>
        )
      })}
    </div>
  )
}
