import { httpClient } from '@/shared/api'
import type {
  EventDto,
  RunResultDto,
  RunSimulationRequestDto,
  StartContinuousRunResultDto,
  StopSimulationRunResultDto,
} from '../model/types'

export async function runSimulation(
  request: RunSimulationRequestDto,
): Promise<RunResultDto> {
  return httpClient.request<RunResultDto>('/api/simulation/runs', {
    method: 'POST',
    body: JSON.stringify(request),
    headers: { 'Content-Type': 'application/json' },
  })
}

/**
 * Start continuous simulation run. Returns result with success false on 409 (another run already running).
 */
export async function startContinuousRun(
  request: RunSimulationRequestDto,
): Promise<StartContinuousRunResultDto> {
  const url = `${httpClient.baseUrl}/api/simulation/runs/start`
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  const data = (await response.json()) as StartContinuousRunResultDto
  if (response.status === 201 || response.status === 409) {
    return data
  }
  throw new Error(data.message ?? `API request failed: ${response.statusText}`)
}

export async function stopRun(runId: string): Promise<StopSimulationRunResultDto> {
  return httpClient.request<StopSimulationRunResultDto>(
    `/api/simulation/runs/${encodeURIComponent(runId)}/stop`,
    { method: 'POST' },
  )
}

export async function getRunEvents(runId: string): Promise<EventDto[]> {
  return httpClient.request<EventDto[]>(`/api/simulation/runs/${encodeURIComponent(runId)}/events`)
}
