const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

async function parseErrorDetail(response: Response): Promise<string> {
  let detail = response.statusText
  try {
    const raw = (await response.text()).trim()
    if (!raw) return detail
    try {
      const err = JSON.parse(raw) as { message?: string; error?: string }
      const msg = err?.message ?? err?.error
      if (typeof msg === 'string' && msg.length > 0) return msg
    } catch {
      if (raw.length < 200) return raw
    }
  } catch {
    /* ignore */
  }
  return detail
}

async function parseSuccessBody<T>(response: Response): Promise<T> {
  if (response.status === 204 || response.status === 205) {
    return undefined as T
  }
  const text = await response.text()
  const trimmed = text.trim()
  if (trimmed.length === 0) {
    return undefined as T
  }
  try {
    return JSON.parse(trimmed) as T
  } catch {
    throw new Error(
      `Invalid JSON response (${response.status}): ${trimmed.slice(0, 120)}`,
    )
  }
}

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
      const detail = await parseErrorDetail(response)
      throw new Error(detail)
    }

    return parseSuccessBody<T>(response)
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
      const detail = await parseErrorDetail(response)
      throw new Error(detail)
    }

    if (response.status !== 204 && response.status !== 205) {
      await response.text()
    }
  },
}
