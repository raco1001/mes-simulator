# Phase 10 Baseline Checklist

## Goal
- Define a domain-agnostic ontology metamodel for Layer 1.
- Separate closed enums (system-read) from open schemas (human/domain extensible).
- Keep runtime behavior unchanged in Phase 10 and deliver shared contracts only.

## Constraints
- Follow `00-overview.md` Layer dependency rule (P10 must stabilize before P11+).
- Keep naming compatible with existing codebase where `Asset` naming remains.
- Add schema contracts first; runtime simulator implementations are deferred to Phase 12.

## Closed Enums (Fixed in Phase 10)
- `DataType`: `number`, `string`, `boolean`, `datetime`, `array`, `object`
- `SimulationBehavior`: `constant`, `settable`, `rate`, `accumulator`, `derived`
- `Mutability`: `immutable`, `mutable`
- `TraitPersistence`: `permanent`, `durable`, `transient`
- `TraitDynamism`: `static`, `dynamic`, `reactive`
- `TraitCardinality`: `singular`, `enumerable`, `streaming`
- `LinkDirection`: `directed`, `bidirectional`, `hierarchical`
- `LinkTemporality`: `permanent`, `durable`, `event_driven`

## Ontology Admission Criteria
- Identity
- Typed
- Linkable
- Observable

Rule: if 3 or more are satisfied, treat as ontology object.

## Governance Rules (7)
1. `traits` must define 3 axes on all `ObjectTypeSchema`.
2. `PropertyDefinition.key` must be unique within one object type.
3. `required: true` properties must have a value at instance creation.
4. `mutability: immutable` cannot be patched after creation.
5. Invalid `LinkTypeSchema` constraints are not allowed for relationship creation.
6. `classifications.taxonomy` must avoid reserved system words.
7. New `SimulationBehavior` requires a matching simulator implementation.

## Acceptance Checklist
- [ ] Closed enum values are reflected in schemas and docs.
- [ ] `property-definition.json`, `object-type-schema.json`, `link-type-schema.json` exist under `shared/api-schemas`.
- [ ] `freezer`, `battery`, `conveyor` examples validate against schemas.
- [ ] Ontology admission criteria are documented.
- [ ] Governance 7 rules are documented.
- [ ] Runtime code changes are not introduced in this phase.
