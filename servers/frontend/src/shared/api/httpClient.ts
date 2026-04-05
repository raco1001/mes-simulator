const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

/**
 * Shared HTTP client: connection config + generic request only.
 * Domain-specific endpoints and types live in entities.
 */
export const httpClient = {
  baseUrl: API_BASE_URL,

  async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    })

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Resource not found')
      }
      let detail = response.statusText
      try {
        const err = (await response.json()) as { message?: string }
        if (typeof err?.message === 'string' && err.message.length > 0) {
          detail = err.message
        }
      } catch {
        /* non-JSON error body */
      }
      throw new Error(detail)
    }

    return response.json()
  },

  /** 204 No Content 등 본문이 없는 성공 응답용 */
  async requestVoid(endpoint: string, options?: RequestInit): Promise<void> {
    const url = `${this.baseUrl}${endpoint}`
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    })

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Resource not found')
      }
      throw new Error(`API request failed: ${response.statusText}`)
    }
  },
}
