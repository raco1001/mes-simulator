# MongoDB 모델 설계

Factory MES 시스템의 MongoDB 데이터 모델 정의입니다.

## 데이터베이스

- **데이터베이스명**: `factory_mes`

## 컬렉션 구조

### 1. assets

Asset의 메타데이터를 저장하는 컬렉션입니다.

**스키마:**
```javascript
{
  "_id": String,              // Asset ID (예: "freezer-1")
  "type": String,             // Asset 타입 (예: "freezer", "conveyor", "sensor")
  "connections": [String],    // 연결된 다른 asset들의 ID 목록
  "metadata": Object,         // 추가 메타데이터 (자유 형식)
  "createdAt": Date,         // 생성 시각
  "updatedAt": Date          // 마지막 업데이트 시각
}
```

**예시:**
```json
{
  "_id": "freezer-1",
  "type": "freezer",
  "connections": ["conveyor-1"],
  "metadata": {
    "location": "factory-floor-a",
    "capacity": "1000L"
  },
  "createdAt": ISODate("2026-02-18T10:00:00Z"),
  "updatedAt": ISODate("2026-02-18T10:00:00Z")
}
```

**인덱스:**
- `_id`: 기본 unique 인덱스 (자동 생성)
- `type`: Asset 타입별 조회
- `updatedAt`: 최근 업데이트 순 조회

**제약사항:**
- `_id`, `type`, `createdAt` 필수
- `_id`는 Asset ID로 사용되며 고유해야 함

---

### 2. events

Raw 이벤트 로그를 저장하는 컬렉션입니다. Kafka에서 수신한 모든 이벤트를 그대로 저장합니다.

**스키마:**
```javascript
{
  "_id": ObjectId,           // MongoDB 자동 생성
  "assetId": String,         // Asset ID
  "eventType": String,       // 이벤트 타입 (예: "asset.created", "asset.health.updated")
  "timestamp": Date,         // 이벤트 발생 시각
  "payload": Object          // 이벤트 페이로드 (이벤트 타입에 따라 다름)
}
```

**예시:**
```json
{
  "_id": ObjectId("..."),
  "assetId": "freezer-1",
  "eventType": "asset.health.updated",
  "timestamp": ISODate("2026-02-18T10:00:00Z"),
  "payload": {
    "temperature": -5,
    "power": 120,
    "status": "normal"
  }
}
```

**인덱스:**
- `{ assetId: 1, timestamp: -1 }`: 특정 asset의 이벤트를 시간순으로 조회
- `{ eventType: 1 }`: 이벤트 타입별 조회
- `{ timestamp: -1 }`: 시간순 조회

**제약사항:**
- `assetId`, `eventType`, `timestamp` 필수
- TTL 인덱스 (90일 후 자동 삭제)는 선택사항 (주석 처리됨)

---

### 3. states

Asset의 현재 상태를 저장하는 컬렉션입니다. **핵심 컬렉션**으로, Pipeline이 이벤트를 처리하여 계산한 최신 상태를 저장합니다.

**스키마:**
```javascript
{
  "_id": ObjectId,           // MongoDB 자동 생성
  "assetId": String,         // Asset ID (unique)
  "currentTemp": Number,     // 현재 온도 (섭씨)
  "currentPower": Number,    // 현재 전력 소비량 (와트)
  "status": String,          // 현재 상태: "normal" | "warning" | "error"
  "lastEventType": String,   // 마지막으로 이 상태를 업데이트한 이벤트 타입
  "updatedAt": Date,         // 마지막 업데이트 시각
  "metadata": Object         // 추가 상태 메타데이터 (자유 형식)
}
```

**예시:**
```json
{
  "_id": ObjectId("..."),
  "assetId": "freezer-1",
  "currentTemp": -5,
  "currentPower": 120,
  "status": "warning",
  "lastEventType": "asset.health.updated",
  "updatedAt": ISODate("2026-02-18T10:00:00Z"),
  "metadata": {
    "humidity": 45
  }
}
```

**인덱스:**
- `{ assetId: 1 }`: unique 인덱스 (하나의 asset당 하나의 상태만 존재)
- `{ status: 1 }`: 상태별 조회 (예: 모든 warning 상태의 asset 조회)
- `{ updatedAt: -1 }`: 최근 업데이트 순 조회

**제약사항:**
- `assetId`, `updatedAt` 필수
- `assetId`는 unique (하나의 asset당 하나의 상태만 존재)
- `status`는 enum: "normal", "warning", "error"

---

## 데이터 흐름

```
Kafka Event
    ↓
Pipeline (Python)
    ↓
1. events 컬렉션에 Raw 이벤트 저장
2. 이벤트를 기반으로 상태 계산
3. states 컬렉션에 상태 저장/업데이트
    ↓
Backend API
    ↓
Frontend UI
```

## 초기화

컬렉션과 인덱스는 `infrastructure/mongo/init-scripts/init-collections.js` 스크립트를 통해 자동으로 생성됩니다.

**수동 실행:**
```bash
docker exec -i mongodb mongosh -u admin -p admin123 --authenticationDatabase admin factory_mes < infrastructure/mongo/init-scripts/init-collections.js
```

## 참고

- [Event Schemas](../../shared/event-schemas/) - 이벤트 스키마 정의
- [MVP Plan](../../MVP-PLAN.md) - 전체 MVP 계획
