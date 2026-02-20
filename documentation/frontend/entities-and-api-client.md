# 엔티티 및 API 클라이언트

프론트엔드에서 백엔드 API를 호출하기 위한 엔티티(타입·API 함수)와 공통 HTTP 클라이언트 구조입니다.

---

## 공통 HTTP 클라이언트

- **위치**: `src/shared/api/` (또는 `httpClient.ts`)
- **역할**: fetch 기반 request 메서드, baseURL(환경 변수 등), JSON 직렬화·에러 처리
- **사용**: 각 entity의 api 모듈에서 import해 GET/POST/PUT/DELETE 호출

---

## Asset 엔티티

- **타입**: `src/entities/asset/model/types.ts`
  - AssetDto: id, type, connections, metadata, createdAt, updatedAt
  - CreateAssetRequest: type, connections, metadata
  - UpdateAssetRequest: type?, connections, metadata?
- **API**: `src/entities/asset/api/assetApi.ts`
  - getAssets() → GET /api/assets
  - getAssetById(id) → GET /api/assets/{id}
  - createAsset(body) → POST /api/assets
  - updateAsset(id, body) → PUT /api/assets/{id}
- **노출**: `entities/asset/index.ts`에서 타입·함수 export

---

## State 엔티티

- **타입**: StateDto (assetId, currentTemp, currentPower, status, lastEventType, updatedAt, metadata)
- **API**: getStates(), getStateByAssetId(assetId) — GET /api/states, GET /api/states/{assetId}
- **위치**: `entities/state/`

---

## Simulation 엔티티

- **타입**: RunSimulationRequest, RunResult
- **API**: runSimulation(request) — POST /api/simulation/runs
- **위치**: `entities/simulation/`

---

## 스키마 일치

- DTO·Request 타입은 `shared/api-schemas/`의 JSON 스키마(assets.json, relationships.json 등)와 맞춰 유지. 백엔드 계약 변경 시 스키마·타입·API 동시 반영.
