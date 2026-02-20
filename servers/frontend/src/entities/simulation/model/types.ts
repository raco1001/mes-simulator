/**
 * Simulation run request (POST /api/simulation/runs body)
 */
export interface RunSimulationRequestDto {
  triggerAssetId: string
  patch?: StatePatchDto
  maxDepth?: number
}

export interface StatePatchDto {
  currentTemp?: number
  currentPower?: number
  status?: string
  lastEventType?: string
}

/**
 * Simulation run result (POST /api/simulation/runs response)
 */
export interface RunResultDto {
  success: boolean
  runId: string
  message: string
}
