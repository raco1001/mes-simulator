import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from './httpClient'

const mockFetch = vi.fn()
// @ts-expect-error - Mocking global fetch for testing
global.fetch = mockFetch as unknown as typeof fetch

describe('httpClient', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls fetch with baseUrl + endpoint and returns JSON', async () => {
    const data = { id: '1', name: 'test' }
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => data,
    } as Response)

    const result = await httpClient.request<typeof data>('/api/foo')

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/foo'),
      expect.objectContaining({
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    expect(result).toEqual(data)
  })

  it('throws Resource not found on 404', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
      statusText: 'Not Found',
    } as Response)

    await expect(httpClient.request('/api/foo')).rejects.toThrow(
      'Resource not found',
    )
  })

  it('throws on other non-ok status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
    } as Response)

    await expect(httpClient.request('/api/foo')).rejects.toThrow(
      'API request failed: Internal Server Error',
    )
  })

  it('forwards method and body for POST', async () => {
    const body = { type: 'x', connections: [] }
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => body,
    } as Response)

    await httpClient.request('/api/assets', {
      method: 'POST',
      body: JSON.stringify(body),
    })

    expect(mockFetch).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify(body),
        headers: { 'Content-Type': 'application/json' },
      }),
    )
  })
})
