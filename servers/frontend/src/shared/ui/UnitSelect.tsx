import { useMemo, useState, type CSSProperties } from 'react'

const BASE_UNITS = ['m', 'kg', 's', 'A', 'K', 'W', 'Wh', 'L', 'item']
const DERIVED_UNITS = ['%', 'mps', 'kW', 'kWh', 'itemsPerHour', 'degC', 'degF']

export interface UnitSelectProps {
  value?: string
  onChange: (unit: string) => void
  placeholder?: string
  style?: CSSProperties
  /** 한 줄(폼 행)에 넣을 때: 검색 없이 select만 표시 */
  compact?: boolean
}

const inputStyle: CSSProperties = {
  padding: '4px 8px',
  borderRadius: 4,
  border: '1px solid #555',
  background: '#1a1a1a',
  color: '#fff',
  width: '100%',
  boxSizing: 'border-box',
}

/** flex 행에 넣을 때 부모 너비를 잡아먹지 않도록 */
const compactSelectStyle: CSSProperties = {
  ...inputStyle,
  width: 'auto',
  minWidth: '5.25rem',
  maxWidth: '11rem',
}

export function UnitSelect({
  value,
  onChange,
  placeholder = '단위 선택',
  style,
  compact = false,
}: UnitSelectProps) {
  const [search, setSearch] = useState('')

  const filteredBase = useMemo(
    () => BASE_UNITS.filter((u) => u.toLowerCase().includes(search.toLowerCase())),
    [search],
  )
  const filteredDerived = useMemo(
    () => DERIVED_UNITS.filter((u) => u.toLowerCase().includes(search.toLowerCase())),
    [search],
  )

  if (compact) {
    return (
      <select
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value)}
        aria-label={placeholder}
        style={{ ...compactSelectStyle, ...style }}
      >
        <option value="">{placeholder}</option>
        <optgroup label="기본 단위">
          {BASE_UNITS.map((u) => (
            <option key={u} value={u}>
              {u}
            </option>
          ))}
        </optgroup>
        <optgroup label="파생 단위">
          {DERIVED_UNITS.map((u) => (
            <option key={u} value={u}>
              {u}
            </option>
          ))}
        </optgroup>
      </select>
    )
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4, ...style }}>
      <input
        type="text"
        placeholder="단위 검색..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        style={inputStyle}
      />
      <select
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value)}
        style={inputStyle}
      >
        <option value="">{placeholder}</option>
        {filteredBase.length > 0 && (
          <optgroup label="기본 단위">
            {filteredBase.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </optgroup>
        )}
        {filteredDerived.length > 0 && (
          <optgroup label="파생 단위">
            {filteredDerived.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </optgroup>
        )}
      </select>
    </div>
  )
}
