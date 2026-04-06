/**
 * Simulation run request (POST /api/simulation/runs body)
 */
export interface RunSimulationRequestDto {
  triggerAssetId: string
  patch?: StatePatchDto
  /** BFS depth; omit or ≤0 uses server default (leaf-oriented). */
  maxDepth?: number
  runTick?: number
  /** Continuous engine polling (ms), 1–5000. */
  engineTickIntervalMs?: number
}

export interface StatePatchDto {
  properties?: Record<string, unknown | null>
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

/**
 * Start continuous run result (POST /api/simulation/runs/start response).
 * 201: success true, runId set. 409: success false, message set.
 */
export interface StartContinuousRunResultDto {
  success: boolean
  runId: string
  message?: string
}

/**
 * Stop simulation run result (POST /api/simulation/runs/{runId}/stop response).
 */
export interface StopSimulationRunResultDto {
  success: boolean
  message?: string
}

/**
 * Domain event (GET /api/simulation/runs/{runId}/events item)
 */
export interface EventDto {
  assetId: string
  eventType: string
  occurredAt: string
  simulationRunId?: string
  relationshipId?: string
  /** Present on tick lifecycle events when the backend includes it. */
  runTick?: number
  payload?: Record<string, unknown>
}

/** GET /api/simulation/runs/{runId} */
export interface SimulationOverrideEntryDto {
  assetId: string
  propertyKey: string
  value: unknown
  fromTick: number
  toTick?: number | null
}

export interface SimulationRunDetailDto {
  id: string
  status: string
  startedAt: string
  endedAt?: string | null
  triggerAssetId: string
  trigger?: Record<string, unknown>
  maxDepth?: number
  engineTickIntervalMs?: number
  tickIndex: number
  initialSnapshot?: Record<string, unknown>
  overrides?: SimulationOverrideEntryDto[]
}

/** POST /api/simulation/runs/{runId}/overrides */
export interface AppendSimulationOverrideRequestDto {
  assetId: string
  propertyKey: string
  value: unknown
  fromTick: number
  toTick?: number | null
}

export interface WhatIfPropertyChangeDto {
  key: string
  before?: unknown
  after?: unknown
  delta?: unknown
}

export interface WhatIfObjectDeltaDto {
  objectId: string
  changes: WhatIfPropertyChangeDto[]
}

export interface WhatIfStateSnapshotDto {
  properties: Record<string, unknown>
}

export interface WhatIfResultDto {
  runId: string
  before: Record<string, WhatIfStateSnapshotDto>
  after: Record<string, WhatIfStateSnapshotDto>
  deltas: WhatIfObjectDeltaDto[]
  affectedObjects: string[]
  propagationDepth: number
}
