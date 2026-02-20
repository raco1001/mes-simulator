# API Schemas

Factory MES 시스템의 REST API 스키마 정의입니다.

## 구조

```
api-schemas/
├── assets.json         # Asset 관련 API 스키마
├── relationships.json  # Relationship(에셋 간 관계) API 스키마
└── README.md
```

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
