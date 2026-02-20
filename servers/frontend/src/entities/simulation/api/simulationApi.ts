import { httpClient } from '@/shared/api'
import type { RunResultDto } from '../model/types'

export async function runSimulation(): Promise<RunResultDto> {
  return httpClient.request<RunResultDto>('/api/simulation/run', {
    method: 'POST',
  })
}
