# States 및 Events API

에셋 현재 상태(states) 조회와 이벤트(events) 저장·조회 관련 백엔드 구현 요약입니다.

---

## States (에셋 현재 상태)

- **경로**: `api/states`
- **역할**: Pipeline이 계산한 에셋별 최신 상태 조회
- **저장소**: MongoDB `factory_mes.states` (assetId당 1건, unique)

### API

- `GET /api/states` — 전체 상태 목록
- `GET /api/states/{assetId}` — 특정 에셋 상태

### 도메인·인프라

- StateDto: assetId, currentTemp, currentPower, status, lastEventType, updatedAt, metadata
- IAssetRepository에 GetAllStatesAsync, GetStateByAssetIdAsync 포함. MongoAssetRepository가 `states` 컬렉션 사용
- StateController에서 IGetStatesQuery(GetStatesQueryHandler) 호출

---

## Events (이벤트 로그)

- **역할**: Raw 이벤트 및 시뮬레이션 런에서 발생한 이벤트 저장
- **저장소**: MongoDB `factory_mes.events`

### 스키마(문서)

- assetId, eventType, timestamp (필수)
- simulationRunId, relationshipId, occurredAt, payload (선택)
- 인덱스: assetId+timestamp, eventType, timestamp, simulationRunId

### 백엔드 사용처

- 시뮬레이션 런 시 전파·룰 적용 결과를 이벤트로 저장할 때 IEventRepository 사용
- Pipeline은 Kafka에서 이벤트를 소비해 별도 처리 (백엔드 REST API와는 별개)

---

## 참고

- [infrastructure/mongo/MODEL.md](../../infrastructure/mongo/MODEL.md) — states, events 컬렉션 상세 스키마
- [asset-command-and-query.md](./asset-command-and-query.md) — Asset CRUD와 연동
