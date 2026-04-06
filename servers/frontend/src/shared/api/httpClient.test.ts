import { describe, it, expect, vi, beforeEach } from 'vitest'
import { httpClient } from './httpClient'

const mockFetch = vi.fn()
// @ts-expect-error - Mocking global fetch for testing
global.fetch = mockFetch as unknown as typeof fetch

function okResponse(
  status: number,
  bodyText: string,
): Pick<Response, 'ok' | 'status' | 'statusText' | 'text'> {
  return {
    ok: true,
    status,
    statusText: 'OK',
    text: async () => bodyText,
  } as Response
}

describe('httpClient', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls fetch with baseUrl + endpoint and returns parsed JSON', async () => {
    const data = { id: '1', name: 'test' }
    mockFetch.mockResolvedValueOnce(okResponse(200, JSON.stringify(data)))

    const result = await httpClient.request<typeof data>('/api/foo')

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/foo'),
      expect.objectContaining({
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    expect(result).toEqual(data)
  })

  it('returns undefined for 204 No Content', async () => {
    mockFetch.mockResolvedValueOnce(okResponse(204, ''))

    const result = await httpClient.request<unknown>('/api/foo')

    expect(result).toBeUndefined()
  })

  it('returns undefined for 200 with empty body', async () => {
    mockFetch.mockResolvedValueOnce(okResponse(200, '  \n  '))

    const result = await httpClient.request<unknown>('/api/foo')

    expect(result).toBeUndefined()
  })

  it('throws descriptive error for non-JSON success body', async () => {
    mockFetch.mockResolvedValueOnce(okResponse(200, 'Stopped'))

    await expect(httpClient.request('/api/foo')).rejects.toThrow(
      /Invalid JSON response/,
    )
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
      text: async () => '',
    } as Response)

    await expect(httpClient.request('/api/foo')).rejects.toThrow(
      'Internal Server Error',
    )
  })

  it('forwards method and body for POST', async () => {
    const body = { type: 'x', connections: [] }
    mockFetch.mockResolvedValueOnce(
      okResponse(200, JSON.stringify(body)),
    )

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

  describe('requestVoid', () => {
    it('resolves on 204', async () => {
      mockFetch.mockResolvedValueOnce(okResponse(204, ''))

      await expect(httpClient.requestVoid('/api/foo')).resolves.toBeUndefined()
    })

    it('throws Resource not found on 404', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
      } as Response)

      await expect(httpClient.requestVoid('/api/foo')).rejects.toThrow(
        'Resource not found',
      )
    })
  })
})
