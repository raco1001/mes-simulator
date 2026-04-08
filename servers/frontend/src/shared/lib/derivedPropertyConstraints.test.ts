import { describe, expect, it } from 'vitest'
import {
  parseDependsOnCsv,
  readDerivedOperation,
  readDerivedDependsOn,
} from './derivedPropertyConstraints'

describe('parseDependsOnCsv', () => {
  it('splits trims and drops empties', () => {
    expect(parseDependsOnCsv(' a , b , ')).toEqual(['a', 'b'])
    expect(parseDependsOnCsv('')).toEqual([])
    expect(parseDependsOnCsv('x')).toEqual(['x'])
  })
})

describe('readDerivedOperation', () => {
  it('defaults invalid or missing to sum', () => {
    expect(readDerivedOperation({})).toBe('sum')
    expect(readDerivedOperation({ constraints: { operation: 'nope' } })).toBe(
      'sum',
    )
  })

  it('accepts known operations case-insensitively', () => {
    expect(readDerivedOperation({ constraints: { operation: 'AVG' } })).toBe(
      'avg',
    )
  })
})

describe('readDerivedDependsOn', () => {
  it('formats array to csv', () => {
    expect(
      readDerivedDependsOn({
        constraints: { dependsOn: ['in1', ' in2 '] },
      }),
    ).toBe('in1, in2')
  })

  it('returns empty when not an array', () => {
    expect(
      readDerivedDependsOn({ constraints: { dependsOn: 'x' as unknown } }),
    ).toBe('')
  })
})
