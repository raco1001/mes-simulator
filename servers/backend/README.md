# Backend (DotnetEngine)

C# / ASP.NET Core 백엔드입니다. Hexagonal architecture 기반으로 Asset, Relationship, Simulation, Event 흐름을 제공합니다.  
C# / ASP.NET Core backend using hexagonal architecture for asset, relationship, simulation, and event flows.

## 실행 / Run

```bash
dotnet run --project DotnetEngine/DotnetEngine.csproj
```

기본 엔드포인트 / Default endpoints:
- `GET http://localhost:5000/api/health`
- `GET http://localhost:5000/api/assets`
- `GET http://localhost:5000/api/simulation/runs`

## 테스트 / Test

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj
```

시뮬레이션 핵심 검증은 `DotnetEngine.Tests/Application/Simulation/`에 집중되어 있습니다.  
Core simulation coverage is concentrated under `DotnetEngine.Tests/Application/Simulation/`.

## 모듈 구성 / Module Overview

### Health
- Domain: health constants/value objects
- Application: `IGetHealthQuery`, handler
- Presentation: `HealthController`

### Asset + Relationship
- Asset CRUD, state read/write, delete guard/conflict handling
- Mongo repositories for assets/states/relationships

### Simulation
- Run: single run + continuous run + stop + what-if
- Multi-seed request support: `triggerAssetIds` (legacy `triggerAssetId` kept for compatibility)
- Behavior engine: `constant`, `settable`, `rate`, `accumulator`, `derived`
- Relationship propagation rules: `Supplies`, `ConnectedTo`, `Contains`

## 현재 구현 상태 / Current Implementation Status

- 완료 / Completed:
  - Ontology contract baseline (phase 10 alignment)
  - Pipeline/recommendation integration path (phase 14, 15 alignment)
  - What-if dry-run and apply loop foundations
- 진행중 / In progress:
  - Simulation governance hardening (phase 21)
  - UX-linked trigger semantics and run visibility (phase 22/23)

## API/Schema 연동 / Contract and Schema Alignment

- HTTP source of truth: `../../shared/api-schemas/openapi.json`
- Ontology schemas: `../../shared/ontology-schemas/`
- Event schemas/topics: `../../shared/event-schemas/`

DTO/Controller 변경 시 위 shared contract를 먼저 갱신하고 백엔드 구현을 동기화합니다.  
Update shared contracts first when changing DTO/controller behavior.

## 환경 설정 / Environment

- Development: `DotnetEngine/appsettings.Development.json`
- Production: `DotnetEngine/appsettings.json` + environment variables
- Mongo connection key: `ConnectionStrings__MongoDB`
- Kafka keys: `Kafka__BootstrapServers`, topic/group settings

상세 환경 설정은 `../../documentation/backend/environment-configuration.md`를 참고하세요.  
See `../../documentation/backend/environment-configuration.md` for full environment setup.

## 트러블슈팅 / Troubleshooting

- Mongo 연결 실패: connection string + `authSource=admin` 확인
- Kafka 발행/소비 불일치: topic 이름(`factory.asset.events`, `factory.asset.alert`) 확인
- 시뮬레이션 전파 기대치 불일치:
  - trigger seed(`triggerAssetIds`) 구성 확인
  - relationship mapping `fromProperty`와 state/metadata key 정합 확인
  - orphan relationship 여부 확인 (`fromAssetId`/`toAssetId`가 실제 asset `_id`인지)
