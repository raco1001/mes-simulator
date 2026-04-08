import type { ExtraProperty } from '@/entities/asset'
import type {
  DataType,
  Mutability,
  SimulationBehavior,
} from '@/entities/object-type-schema'
import {
  DERIVED_OPERATIONS,
  parseDependsOnCsv,
  readDerivedDependsOn,
  readDerivedOperation,
} from '@/shared/lib/derivedPropertyConstraints'
import { DerivedDependsOnTextInput } from '@/shared/ui/DerivedDependsOnTextInput'
import { UnitSelect } from '@/shared/ui/UnitSelect'

const DATA_TYPES = [
  'Number',
  'String',
  'Boolean',
  'DateTime',
  'Array',
  'Object',
] as const satisfies readonly DataType[]

const SIMULATION_BEHAVIORS = [
  'Constant',
  'Settable',
  'Rate',
  'Accumulator',
  'Derived',
] as const satisfies readonly SimulationBehavior[]

const MUTABILITY_OPTIONS: Mutability[] = ['Mutable', 'Immutable']

export function ExtraPropertiesSection({
  extraProperties,
  onAdd,
  onUpdate,
  onRemove,
  resetKey = '',
}: {
  extraProperties: ExtraProperty[]
  onAdd: () => void
  onUpdate: (index: number, patch: Partial<ExtraProperty>) => void
  onRemove: (index: number) => void
  /** 에셋/모달 전환 시 dependsOn 입력 draft 초기화용 */
  resetKey?: string
}) {
  return (
    <div className="assets-canvas-page__meta-section">
      <span>확장 속성 (extraProperties)</span>
      <span className="assets-canvas-page__meta-section-hint">
        시뮬 동작·변경 가능 여부는 백엔드 엔진에 반영됩니다.
      </span>
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
            <select
              value={p.simulationBehavior}
              onChange={(e) =>
                onUpdate(i, {
                  simulationBehavior: e.target.value as SimulationBehavior,
                })
              }
              aria-label={`extra-prop-behavior-${i}`}
              title="시뮬레이션 동작 (엔진에 반영)"
            >
              {SIMULATION_BEHAVIORS.map((sb) => (
                <option key={sb} value={sb}>
                  {sb}
                </option>
              ))}
            </select>
            <select
              value={p.mutability}
              onChange={(e) =>
                onUpdate(i, { mutability: e.target.value as Mutability })
              }
              aria-label={`extra-prop-mutability-${i}`}
              title="Immutable이면 패치로 값 변경 불가"
            >
              {MUTABILITY_OPTIONS.map((m) => (
                <option key={m} value={m}>
                  {m}
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
          {p.simulationBehavior === 'Derived' && p.dataType === 'Number' ? (
            <div className="assets-canvas-page__meta-row-derived-extra">
              <DerivedDependsOnTextInput
                syncKey={`${resetKey}-${i}`}
                canonicalCsv={readDerivedDependsOn(p)}
                placeholder="dependsOn (콤마 구분 속성 키)"
                onCsvChange={(csv) =>
                  onUpdate(i, {
                    constraints: {
                      ...(p.constraints ?? {}),
                      dependsOn: parseDependsOnCsv(csv),
                    },
                  })
                }
                aria-label={`extra-prop-derived-dependson-${i}`}
                title="이 에셋 상태에 존재하는 속성 키 이름을 콤마로 구분해 입력"
              />
              <select
                value={readDerivedOperation(p)}
                onChange={(e) =>
                  onUpdate(i, {
                    constraints: {
                      ...(p.constraints ?? {}),
                      operation: e.target.value,
                    },
                  })
                }
                aria-label={`extra-prop-derived-operation-${i}`}
                title="집계 방식"
              >
                {DERIVED_OPERATIONS.map((op) => (
                  <option key={op} value={op}>
                    {op}
                  </option>
                ))}
              </select>
            </div>
          ) : null}
          <input
            placeholder="초기값 (BaseValue)"
            value={String(p.value ?? '')}
            onChange={(e) => onUpdate(i, { value: e.target.value })}
            aria-label={`extra-prop-value-${i}`}
            title="상태가 없을 때 시드값"
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
