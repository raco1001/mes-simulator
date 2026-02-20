# Relationship API (에셋 간 관계)

에셋 간 관계를 first-class 엔티티로 저장·조회·수정·삭제하는 API 구현 내용입니다. Phase 1(ADR-20260220) 반영.

---

## 개요

- **경로**: `api/relationships`
- **역할**: 관계의 종류(`relationshipType`), 방향(`fromAssetId` → `toAssetId`), 관계 단위 속성(`properties`)을 저장·조회·수정·삭제
- **저장소**: MongoDB `factory_mes.relationships` 컬렉션

---

## API 엔드포인트

| 메서드 | 경로 | 설명 |
|--------|------|------|
| GET | `/api/relationships` | 전체 관계 목록 조회 |
| GET | `/api/relationships/{id}` | 단건 조회 |
| POST | `/api/relationships` | 관계 생성 (201 + body) |
| PUT | `/api/relationships/{id}` | 관계 수정 (200 + body, 404) |
| DELETE | `/api/relationships/{id}` | 관계 삭제 (204, 404) |

요청/응답 스키마는 `shared/api-schemas/relationships.json` 참고.

---

## Domain

- **위치**: `Domain/Relationship/Entities/Relationship.cs`
- **프로퍼티**: Id, FromAssetId, ToAssetId, RelationshipType, Properties(IReadOnlyDictionary<string, object>), CreatedAt, UpdatedAt
- **팩토리**: `Create(id, fromAssetId, toAssetId, relationshipType, properties?)`, `Restore(...)` (영속화 복원)
- **변경**: UpdateFromAssetId, UpdateToAssetId, UpdateRelationshipType, UpdateProperties, TouchUpdatedAt

---

## Application

- **Driven Port**: `IRelationshipRepository` — GetAllAsync, GetByIdAsync, AddAsync(RelationshipDto), UpdateAsync(id, RelationshipDto), DeleteAsync(id)
- **DTO**: RelationshipDto, CreateRelationshipRequest, UpdateRelationshipRequest (`Application/Relationship/Dto/`)
- **Driving Ports**: IGetRelationshipsQuery, ICreateRelationshipCommand, IUpdateRelationshipCommand, IDeleteRelationshipCommand
- **Handlers**: GetRelationshipsQueryHandler, CreateRelationshipCommandHandler, UpdateRelationshipCommandHandler, DeleteRelationshipCommandHandler

생성 시 ID는 `Guid.NewGuid().ToString("N")`으로 부여. Update/Delete는 기존 Asset 패턴(GetById 후 DTO 조합 또는 삭제)과 동일.

---

## Infrastructure

- **Mongo 문서**: `MongoRelationshipDocument` — BsonId Id, BsonElement fromAssetId, toAssetId, relationshipType, properties(BsonDocument), createdAt, updatedAt
- **Repository**: `MongoRelationshipRepository` — 컬렉션명 `relationships`, DB `factory_mes`. Properties 변환에 `MetadataBsonConverter` 사용

---

## Presentation

- **Controller**: `RelationshipController` — Route `api/relationships`, GET(all), GET("{id}"), POST, PUT("{id}"), DELETE("{id}")
- **검증**: POST 시 fromAssetId, toAssetId, relationshipType 비어 있으면 400

---

## DI 등록 (Program.cs)

- IRelationshipRepository → MongoRelationshipRepository (Scoped)
- IGetRelationshipsQuery → GetRelationshipsQueryHandler, ICreateRelationshipCommand → CreateRelationshipCommandHandler, IUpdateRelationshipCommand → UpdateRelationshipCommandHandler, IDeleteRelationshipCommand → DeleteRelationshipCommandHandler (Scoped)
