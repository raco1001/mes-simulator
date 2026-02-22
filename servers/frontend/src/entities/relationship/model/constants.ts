/**
 * Relationship type options for dropdowns (create/edit).
 */
export const RELATIONSHIP_TYPE_OPTIONS = ['feeds_into', 'contains', 'located_in'] as const

export type RelationshipTypeOption = (typeof RELATIONSHIP_TYPE_OPTIONS)[number]
