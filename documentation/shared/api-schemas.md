# API 스키마 (shared/api-schemas)

Backend·Frontend 간 REST API 계약을 정의한 JSON 스키마와 엔드포인트 요약입니다.

---

## 위치 및 구조

- **경로**: `shared/api-schemas/`
- **파일**: `assets.json`, `relationships.json`, `README.md`
- **규약**: JSON Schema draft-07, definitions + paths 형태. Backend Swagger·Frontend 타입과 동기화 유지

---

## assets.json

- **정의**: AssetDto, StateDto, CreateAssetRequest, UpdateAssetRequest
- **경로**:
  - GET/POST /api/assets, GET/PUT /api/assets/{id}
  - GET /api/states, GET /api/states/{assetId}
- **AssetDto**: id, type, connections, metadata, createdAt, updatedAt
  - **metadata** (시뮬레이션 tick용, 선택): `tickIntervalMs` (number, ms), `tickPhaseMs` (number, ms). 0 또는 미설정 시 Run 전역 tick 사용. 자세한 규칙은 [simulation-engine-tick-rules.md](../backend/simulation-engine-tick-rules.md) 참고.
- **CreateAssetRequest**: type(필수), connections, metadata
- **UpdateAssetRequest**: type, connections, metadata (모두 선택)

---

## relationships.json

- **정의**: RelationshipDto, CreateRelationshipRequest, UpdateRelationshipRequest
- **경로**:
  - GET/POST /api/relationships
  - GET/PUT/DELETE /api/relationships/{id}
- **RelationshipDto**: id, fromAssetId, toAssetId, relationshipType, properties, createdAt, updatedAt
- **CreateRelationshipRequest**: fromAssetId, toAssetId, relationshipType(필수), properties(선택)
- **UpdateRelationshipRequest**: fromAssetId, toAssetId, relationshipType, properties (모두 선택)

---

## 사용처

- Backend: Controller 요청/응답 타입·Swagger 문서와 일치시키기 위한 참조
- Frontend: entities/asset, entities/simulation 등에서 DTO·Request 타입 정의 시 스키마 기준
- 새 엔드포인트·필드 추가 시 스키마 먼저 수정 후 Backend·Frontend 반영 권장

---

## 참고

- [infrastructure/mongo/MODEL.md](../../infrastructure/mongo/MODEL.md) — MongoDB 문서 구조 (스키마와 필드 매핑)
- [Event schemas](../../shared/event-schemas/) — 이벤트 페이로드 스키마(별도 디렉터리)
