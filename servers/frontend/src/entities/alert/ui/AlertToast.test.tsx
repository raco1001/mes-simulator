import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AlertToast } from './AlertToast'

describe('AlertToast', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('renders warning severity and closes after 5 seconds', () => {
    const onClose = vi.fn()

    render(
      <AlertToast
        alert={{
          assetId: 'freezer-1',
          timestamp: '2026-03-25T10:20:30Z',
          severity: 'warning',
          message: 'temperature warning',
        }}
        onClose={onClose}
      />,
    )

    expect(screen.getByText('WARNING')).toBeInTheDocument()
    expect(screen.getByText('temperature warning')).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()

    vi.advanceTimersByTime(5000)
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('renders error severity and closes after 10 seconds', () => {
    const onClose = vi.fn()

    render(
      <AlertToast
        alert={{
          assetId: 'freezer-2',
          timestamp: '2026-03-25T10:20:30Z',
          severity: 'error',
          message: 'engine failure',
        }}
        onClose={onClose}
      />,
    )

    expect(screen.getByText('ERROR')).toBeInTheDocument()
    vi.advanceTimersByTime(9999)
    expect(onClose).not.toHaveBeenCalled()
    vi.advanceTimersByTime(1)
    expect(onClose).toHaveBeenCalledTimes(1)
  })
})
