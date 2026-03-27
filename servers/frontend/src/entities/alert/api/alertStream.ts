import type { AlertDto } from '../model/types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const ALERT_STREAM_ENDPOINT = '/api/alerts/stream'

export function subscribeAlerts(onAlert: (alert: AlertDto) => void): () => void {
  if (typeof EventSource === 'undefined') {
    return () => {}
  }

  const source = new EventSource(`${API_BASE_URL}${ALERT_STREAM_ENDPOINT}`)

  source.onmessage = (event: MessageEvent<string>) => {
    try {
      const alert = JSON.parse(event.data) as AlertDto
      onAlert(alert)
    } catch {
      // Ignore malformed event payload.
    }
  }

  return () => {
    source.close()
  }
}
