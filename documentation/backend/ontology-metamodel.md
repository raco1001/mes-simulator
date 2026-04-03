# Ontology Metamodel (Phase 10)

## Purpose
- Establish a Layer 1 contract that any domain can use, not only manufacturing.
- Keep implementation simple for current scale: schema-first, data-driven extension.
- Avoid runtime changes in this phase; backend behavior wiring is deferred to Phase 11+.

## Scope
- `PropertyDefinition` schema
- `ObjectTypeSchema` schema
- `LinkTypeSchema` schema
- Example object types (`freezer`, `battery`, `conveyor`)

## Why This Fits Current Scale
- A monolith-first product benefits from strong shared contracts before feature growth.
- Closed enums reduce ambiguity in validation and simulation branching.
- Open data fields (`classifications`, `constraints`, `unit`, `baseValue`) allow domain growth without code churn.

If scale increases, this can evolve into a schema registry + compatibility gate in CI.

## Closed vs Open System

### Closed enums (system-read)
- DataType: `number|string|boolean|datetime|array|object`
- SimulationBehavior: `constant|settable|rate|accumulator|derived`
- Mutability: `immutable|mutable`
- Traits:
  - Persistence: `permanent|durable|transient`
  - Dynamism: `static|dynamic|reactive`
  - Cardinality: `singular|enumerable|streaming`
- Link:
  - Direction: `directed|bidirectional|hierarchical`
  - Temporality: `permanent|durable|event_driven`

### Open data (human/domain-read)
- `classifications[]` and taxonomy values
- `unit`, `baseValue`, `constraints`
- object type and link type names

## Core Contracts

### PropertyDefinition
Path: `shared/api-schemas/property-definition.json`

Required:
- `key`
- `dataType`
- `simulationBehavior`
- `mutability`
- `required`

### ObjectTypeSchema
Path: `shared/api-schemas/object-type-schema.json`

Required:
- `objectType`
- `displayName`
- `traits`
- `properties`

Optional:
- `classifications[]`
- `allowedLinks[]`

`allowedLinks.targetTraits` supports partial trait filters (e.g. only `dynamism`).

### LinkTypeSchema
Path: `shared/api-schemas/link-type-schema.json`

Required:
- `linkType`
- `displayName`
- `direction`
- `temporality`
- `fromConstraint`
- `toConstraint`

`fromConstraint.requiredTraits` and `toConstraint.requiredTraits` support partial trait filters.

## Governance Rules
1. Every object type must define all 3 trait axes.
2. Property keys must be unique within an object type.
3. `required: true` properties must have values at object creation.
4. Immutable properties are not patchable after creation.
5. Link creation must satisfy link type constraints (MVP can warn first, then reject).
6. `classifications.taxonomy` must avoid system-reserved terms.
7. Adding a new `SimulationBehavior` requires matching simulator implementation (`IPropertySimulator`) in Phase 12.

## Validation
Validation script:
- `shared/api-schemas/validate_phase10_examples.py`

Validated fixtures:
- `shared/api-schemas/examples/freezer.json`
- `shared/api-schemas/examples/battery.json`
- `shared/api-schemas/examples/conveyor.json`

Negative fixtures:
- `shared/api-schemas/examples/invalid/invalid-missing-required.json`
- `shared/api-schemas/examples/invalid/invalid-enum.json`

## Phase 11 Handoff
- Wire `ObjectTypeSchema` to object instance creation/update validation.
- Map dynamic properties to runtime state validation.
- Enforce trait-based checks at API boundary before simulation logic.
