import { useMemo, useState, type CSSProperties } from 'react'

const BASE_UNITS = ['m', 'kg', 's', 'A', 'K', 'W', 'Wh', 'L', 'item']
const DERIVED_UNITS = ['%', 'mps', 'kW', 'kWh', 'itemsPerHour', 'degC', 'degF']

export interface UnitSelectProps {
  value?: string
  onChange: (unit: string) => void
  placeholder?: string
  style?: CSSProperties
}

export function UnitSelect({
  value,
  onChange,
  placeholder = '단위 선택',
  style,
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

  const inputStyle: CSSProperties = {
    padding: '4px 8px',
    borderRadius: 4,
    border: '1px solid #555',
    background: '#1a1a1a',
    color: '#fff',
    width: '100%',
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
