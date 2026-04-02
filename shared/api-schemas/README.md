# API Schemas

Factory MES 시스템의 REST API 스키마 정의입니다.

## 구조

```
api-schemas/
├── openapi.json         # OpenAPI 3.0.3 기준 계약 (권장)
├── assets.json          # 레거시 스키마 (유지)
├── state.json           # 레거시 스키마 (유지)
├── relationships.json   # 레거시 스키마 (유지)
└── README.md
```

> **온톨로지 메타모델 스키마는 이 디렉토리에 없습니다.**
> `ObjectTypeSchema`, `LinkTypeSchema`, `PropertyDefinition` 등 Layer 1 메타계층 계약은
> [`shared/ontology-schemas/`](../ontology-schemas/README.md)에 위치합니다.
> Phase 11에서 `/api/object-type-schemas` 엔드포인트 추가 시 `openapi.json`이 해당 스키마를 소스로 동기화합니다.

## 기준 계약

- 현재 REST API 기준 문서는 `openapi.json` 입니다.
- `assets.json`, `state.json`, `relationships.json`은 레거시 호환 목적의 참고 자료로 유지합니다.
- 신규 변경은 `openapi.json`을 우선 수정하고, 필요 시 레거시 파일을 보조 업데이트합니다.

## API 엔드포인트

### Assets

- `GET /api/assets` - 모든 asset 목록 조회
- `GET /api/assets/{id}` - 특정 asset 정보 조회
- `POST /api/assets` - Asset 생성
- `PUT /api/assets/{id}` - 특정 asset 수정

### Relationships

- `GET /api/relationships` - 모든 relationship 목록 조회
- `GET /api/relationships/{id}` - 특정 relationship 조회
- `POST /api/relationships` - Relationship 생성
- `PUT /api/relationships/{id}` - 특정 relationship 수정
- `DELETE /api/relationships/{id}` - 특정 relationship 삭제

### States

- `GET /api/states` - 모든 asset의 현재 상태 조회
- `GET /api/states/{assetId}` - 특정 asset의 현재 상태 조회

## 스키마 정의

### AssetDto

```json
{
  "id": "freezer-1",
  "type": "freezer",
  "connections": ["conveyor-1"],
  "metadata": {
    "location": "factory-floor-a",
    "capacity": "1000L"
  },
  "createdAt": "2026-02-18T10:00:00Z",
  "updatedAt": "2026-02-18T10:00:00Z"
}
```

### StateDto

```json
{
  "assetId": "freezer-1",
  "currentTemp": -5.0,
  "currentPower": 120.0,
  "status": "normal",
  "lastEventType": "asset.health.updated",
  "updatedAt": "2026-02-18T10:00:00Z",
  "metadata": {
    "humidity": 45
  }
}
```

### RelationshipDto

```json
{
  "id": "rel-001",
  "fromAssetId": "freezer-1",
  "toAssetId": "conveyor-1",
  "relationshipType": "feeds_into",
  "properties": { "flowRate": 100 },
  "createdAt": "2026-02-20T10:00:00Z",
  "updatedAt": "2026-02-20T10:00:00Z"
}
```

## 참고

- [MongoDB 모델](../../infrastructure/mongo/MODEL.md) - 데이터 모델 정의
- [Event schemas](../../shared/event-schemas/) - 이벤트 스키마 정의
- [FIELD-MAPPING.md](../FIELD-MAPPING.md) - Kafka 이벤트 필드와 REST API 필드명 매핑
