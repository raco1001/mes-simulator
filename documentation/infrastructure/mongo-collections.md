# MongoDB 컬렉션 (factory_mes)

Factory MES 백엔드가 사용하는 MongoDB 데이터베이스 및 컬렉션 정의·초기화 요약입니다. ADR·planning 제외, 구현 기준 정리.

---

## 데이터베이스

- **이름**: `factory_mes`
- **초기화**: `infrastructure/mongo/init-scripts/init-collections.js` — docker-compose 등에서 `/docker-entrypoint-initdb.d`로 마운트 시 최초 기동 시 1회 실행

---

## 컬렉션 목록

| 컬렉션 | 용도 | 필수 필드 요약 |
|--------|------|----------------|
| assets | 에셋 메타데이터 | _id, type, createdAt |
| events | Raw·시뮬레이션 이벤트 로그 | assetId, eventType, timestamp |
| states | 에셋 현재 상태(1 asset 1 document) | assetId, updatedAt |
| relationships | 에셋 간 관계(방향·타입·속성) | _id, fromAssetId, toAssetId, relationshipType, createdAt, updatedAt |
| simulation_runs | 시뮬레이션 런 세션 | _id, startedAt, triggerAssetId, maxDepth |

---

## assets

- _id(string), type, connections(array of string), metadata(object), createdAt, updatedAt
- 인덱스: type, updatedAt(-1)

---

## events

- assetId, eventType, timestamp(필수); simulationRunId, relationshipId, occurredAt, payload(선택)
- 인덱스: assetId+timestamp(-1), eventType, timestamp(-1), simulationRunId(1)

---

## states

- assetId(unique), currentTemp, currentPower, status, lastEventType, updatedAt, metadata
- 인덱스: assetId(unique), status, updatedAt(-1)

---

## relationships

- _id, fromAssetId, toAssetId, relationshipType, properties(object, 선택), createdAt, updatedAt
- 인덱스: fromAssetId, toAssetId, relationshipType, updatedAt(-1), (fromAssetId, toAssetId) 복합

---

## simulation_runs

- _id, startedAt, endedAt(선택), triggerAssetId, trigger(선택), maxDepth
- 인덱스: startedAt(-1), triggerAssetId(1)

---

## 초기화 스크립트

- **파일**: `infrastructure/mongo/init-scripts/init-collections.js`
- **실행**: DB 최초 생성 시에만 실행됨(데이터 디렉터리 비어 있을 때). 기존 볼륨 유지 시 수동으로 스크립트 적용하거나 컬렉션/인덱스만 추가 가능
- **상세 스키마·예시**: [infrastructure/mongo/MODEL.md](../../infrastructure/mongo/MODEL.md)
