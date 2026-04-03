export interface SimulationTickEvent {
  runId: string
  tick: number
  assetId: string
  properties: Record<string, unknown>
  status: string
  timestamp: string
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const SIMULATION_STREAM_ENDPOINT = '/api/simulation/stream'

export function subscribeSimulationEvents(
  onEvent: (event: SimulationTickEvent) => void,
): () => void {
  if (typeof EventSource === 'undefined') {
    return () => {}
  }

  const source = new EventSource(`${API_BASE_URL}${SIMULATION_STREAM_ENDPOINT}`)

  source.onmessage = (event: MessageEvent<string>) => {
    try {
      const tickEvent = JSON.parse(event.data) as SimulationTickEvent
      onEvent(tickEvent)
    } catch {
      // Ignore malformed event payload.
    }
  }

  return () => {
    source.close()
  }
}
