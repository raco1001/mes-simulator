export interface AlertDto {
  assetId: string
  timestamp: string
  severity: string
  message: string
  runId?: string | null
  metric?: string | null
  current?: number | null
  threshold?: number | null
  code?: string | null
  metrics?: Array<{
    metric: string
    current: number
    threshold: number
    code: string
    severity: string
  }>
}
