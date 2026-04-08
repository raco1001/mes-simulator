import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DerivedDependsOnTextInput } from './DerivedDependsOnTextInput'

describe('DerivedDependsOnTextInput', () => {
  it('keeps trailing comma while typing (controlled round-trip would strip it)', async () => {
    const user = userEvent.setup()
    const onCsvChange = vi.fn()

    render(
      <DerivedDependsOnTextInput
        syncKey="row-0"
        canonicalCsv=""
        onCsvChange={onCsvChange}
        aria-label="depends-on"
      />,
    )

    const input = screen.getByLabelText('depends-on')
    await user.type(input, 'a,b')
    expect(input).toHaveValue('a,b')
    expect(onCsvChange.mock.calls.map((c) => c[0])).toContain('a,')
  })
})
