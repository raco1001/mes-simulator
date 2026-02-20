import { httpClient } from '@/shared/api'
import type { RunResultDto, RunSimulationRequestDto } from '../model/types'

export async function runSimulation(
  request: RunSimulationRequestDto,
): Promise<RunResultDto> {
  return httpClient.request<RunResultDto>('/api/simulation/runs', {
    method: 'POST',
    body: JSON.stringify(request),
    headers: { 'Content-Type': 'application/json' },
  })
}
