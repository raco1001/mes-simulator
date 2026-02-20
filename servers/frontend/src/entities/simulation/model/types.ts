/**
 * Simulation run result (POST /api/simulation/run response)
 */

export interface RunResultDto {
  success: boolean
  message: string
  assetsCount: number
  relationshipsCount: number
}
