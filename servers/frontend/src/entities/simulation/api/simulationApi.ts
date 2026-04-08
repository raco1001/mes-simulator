import { httpClient } from '@/shared/api'
import type {
  AppendSimulationOverrideRequestDto,
  EventDto,
  RunResultDto,
  RunSimulationRequestDto,
  SimulationRunDetailDto,
  StartContinuousRunResultDto,
  StopSimulationRunResultDto,
  WhatIfResultDto,
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

/** GET /api/simulation/running — 실행 중(백엔드 Status=Running) 런 목록 */
export async function getRunningSimulationRuns(): Promise<SimulationRunDetailDto[]> {
  const data = await httpClient.request<SimulationRunDetailDto[] | undefined>(
    '/api/simulation/running',
  )
  return Array.isArray(data) ? data : []
}


export async function getRun(runId: string): Promise<SimulationRunDetailDto> {
  return httpClient.request<SimulationRunDetailDto>(
    `/api/simulation/runs/${encodeURIComponent(runId)}`,
  )
}

export async function appendSimulationOverride(
  runId: string,
  body: AppendSimulationOverrideRequestDto,
): Promise<void> {
  await httpClient.requestVoid(
    `/api/simulation/runs/${encodeURIComponent(runId)}/overrides`,
    {
      method: 'POST',
      body: JSON.stringify(body),
      headers: { 'Content-Type': 'application/json' },
    },
  )
}

export async function getRunEvents(runId: string): Promise<EventDto[]> {
  return httpClient.request<EventDto[]>(`/api/simulation/runs/${encodeURIComponent(runId)}/events`)
}

export async function runWhatIf(
  request: RunSimulationRequestDto,
): Promise<WhatIfResultDto> {
  return httpClient.request<WhatIfResultDto>('/api/simulation/what-if', {
    method: 'POST',
    body: JSON.stringify(request),
    headers: { 'Content-Type': 'application/json' },
  })
}
