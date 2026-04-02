import { describe, it, expect, vi, beforeEach } from 'vitest'
import { subscribeAlerts } from './alertStream'
import type { AlertDto } from '../model/types'

class MockEventSource {
  public static instances: MockEventSource[] = []
  public onmessage: ((event: MessageEvent<string>) => void) | null = null
  public closed = false
  public readonly url: string

  constructor(url: string) {
    this.url = url
    MockEventSource.instances.push(this)
  }

  close() {
    this.closed = true
  }
}

describe('alertStream', () => {
  beforeEach(() => {
    MockEventSource.instances = []
    vi.stubGlobal('EventSource', MockEventSource)
  })

  it('subscribes to SSE endpoint and forwards parsed alerts', () => {
    const onAlert = vi.fn<(alert: AlertDto) => void>()
    const unsubscribe = subscribeAlerts(onAlert)
    const source = MockEventSource.instances[0]

    expect(source).toBeDefined()
    expect(source.url).toContain('/api/alerts/stream')

    source.onmessage?.({
      data: JSON.stringify({
        assetId: 'freezer-1',
        timestamp: '2026-03-25T10:20:30Z',
        severity: 'warning',
        message: 'high temp',
      }),
    } as MessageEvent<string>)

    expect(onAlert).toHaveBeenCalledTimes(1)
    expect(onAlert).toHaveBeenCalledWith(
      expect.objectContaining({
        assetId: 'freezer-1',
        severity: 'warning',
      }),
    )

    unsubscribe()
    expect(source.closed).toBe(true)
  })

  it('ignores malformed payloads', () => {
    const onAlert = vi.fn<(alert: AlertDto) => void>()
    subscribeAlerts(onAlert)
    const source = MockEventSource.instances[0]

    source.onmessage?.({ data: '{not-json}' } as MessageEvent<string>)

    expect(onAlert).not.toHaveBeenCalled()
  })
})
