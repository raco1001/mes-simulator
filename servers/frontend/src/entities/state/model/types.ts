export interface StateDto {
  assetId: string
  properties: Record<string, unknown>
  status: 'normal' | 'warning' | 'error'
  lastEventType: string | null
  updatedAt: string
  metadata: Record<string, unknown>
}
