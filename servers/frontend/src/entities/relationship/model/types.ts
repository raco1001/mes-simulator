/**
 * Relationship API response (GET /api/relationships, GET /api/relationships/:id)
 */
export interface RelationshipDto {
  id: string
  fromAssetId: string
  toAssetId: string
  relationshipType: string
  properties: Record<string, unknown>
  createdAt: string
  updatedAt: string
}

/** Create request (POST /api/relationships) */
export interface CreateRelationshipRequest {
  fromAssetId: string
  toAssetId: string
  relationshipType: string
  properties?: Record<string, unknown>
}

/** Update request (PUT /api/relationships/:id) */
export interface UpdateRelationshipRequest {
  fromAssetId?: string
  toAssetId?: string
  relationshipType?: string
  properties?: Record<string, unknown>
}
