export interface StateDto {
  assetId: string
  currentTemp: number | null
  currentPower: number | null
  status: 'normal' | 'warning' | 'error'
  lastEventType: string | null
  updatedAt: string
  metadata: Record<string, unknown>
}
