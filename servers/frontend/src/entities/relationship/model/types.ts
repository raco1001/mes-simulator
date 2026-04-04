/** Supplies propagation: map source asset property to target (OpenAPI PropertyMapping). */
export interface PropertyMapping {
  fromProperty: string
  toProperty: string
  transformRule?: string
  fromUnit?: string | null
  toUnit?: string | null
}

/**
 * Relationship API response (GET /api/relationships, GET /api/relationships/:id)
 */
export interface RelationshipDto {
  id: string
  fromAssetId: string
  toAssetId: string
  relationshipType: string
  properties: Record<string, unknown>
  mappings?: PropertyMapping[]
  createdAt: string
  updatedAt: string
}

/** Create request (POST /api/relationships) */
export interface CreateRelationshipRequest {
  fromAssetId: string
  toAssetId: string
  relationshipType: string
  properties?: Record<string, unknown>
  mappings?: PropertyMapping[]
}

/** Update request (PUT /api/relationships/:id) */
export interface UpdateRelationshipRequest {
  fromAssetId?: string
  toAssetId?: string
  relationshipType?: string
  properties?: Record<string, unknown>
  mappings?: PropertyMapping[] | null
}
