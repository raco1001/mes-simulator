import { httpClient } from '@/shared/api'
import type { EventDto, RunResultDto, RunSimulationRequestDto } from '../model/types'

export async function runSimulation(
  request: RunSimulationRequestDto,
): Promise<RunResultDto> {
  return httpClient.request<RunResultDto>('/api/simulation/runs', {
    method: 'POST',
    body: JSON.stringify(request),
    headers: { 'Content-Type': 'application/json' },
  })
}

export async function getRunEvents(runId: string): Promise<EventDto[]> {
  return httpClient.request<EventDto[]>(`/api/simulation/runs/${encodeURIComponent(runId)}/events`)
}
