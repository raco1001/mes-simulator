export type RecommendationSeverity = 'info' | 'warning' | 'critical'
export type RecommendationStatus = 'pending' | 'approved' | 'rejected' | 'applied'

export interface RecommendationDto {
  recommendationId: string
  objectId: string
  objectType: string
  severity: RecommendationSeverity
  category: string
  title: string
  description: string
  suggestedAction: Record<string, unknown>
  analysisBasis: Record<string, unknown>
  status: RecommendationStatus
  createdAt: string
  updatedAt: string
}
