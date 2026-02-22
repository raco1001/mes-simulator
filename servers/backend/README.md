# dotnet-engine

C# / ASP.NET Core 백엔드 (Windows Server 배포 예정).  
Layered + Hexagonal Architecture, Health Check 모듈 템플릿 적용.

## 실행

```bash
dotnet run --project DotnetEngine/DotnetEngine.csproj
# GET http://localhost:5000/api/health
# GET http://localhost:5000/api/assets
```

## 테스트

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj
```

## 모듈 구성

### Health 모듈 (템플릿)

- **Domain**: `HealthConstants`, `HealthStatusKind`, `HealthReport` (Value Object)
- **Port**: `IGetHealthQuery` (Application)
- **Adapter**: `HealthController` (Presentation) — `GET /api/health`
- **Use Case**: `GetHealthQueryHandler` — 도메인 HealthReport를 DTO로 반환

### Asset 모듈 (모델 정의·저장)

- **Domain**: `Asset`, `AssetState` (Entities), `AssetConstants`
- **Port**: `IGetAssetsQuery`, `IGetStatesQuery`, `ICreateAssetCommand`, `IUpdateAssetCommand`
- **Adapter**: `AssetController` (Presentation) — `GET/POST /api/assets`, `GET/PUT /api/assets/{id}` / `StateController` — `GET /api/states`, `GET /api/states/{assetId}`
- **Use Case**: GetAssetsQueryHandler, GetStatesQueryHandler, CreateAssetCommandHandler, UpdateAssetCommandHandler

기획·구현 결과·실무 확장 요소는 **[documentation/backend/asset-command-and-query.md](../../documentation/backend/asset-command-and-query.md)** 에 정리되어 있음.

### Simulation 모듈 (시뮬레이션 런·이벤트)

- **Domain** (`Domain/Simulation/`): `EventKind`, `SimulationRunStatus` (ValueObjects), `EventTypes` (Constants). 이벤트 계열(Command/Observation) 및 런 상태 정의.
- **Application**: Ports(Driving/Driven), Handlers, Dto, Rules, Workers(`SimulationEngineService`). Use Case 및 주기적 전파 오케스트레이션(Workers).
- **Adapter**: SimulationController, MongoSimulationRunRepository, KafkaEventPublisher.

새 모듈 추가 시 동일하게 Port → 테스트 선행 → Handler/Adapter 구현 순서로 확장할 수 있음.

## 환경 설정

- **WSL(개발)**: `dotnet run` 시 `ASPNETCORE_ENVIRONMENT=Development`, `appsettings.Development.json` 적용. (`DotnetEngine/Properties/launchSettings.json` 참고.)
- **Windows(배포/운영)**: `ASPNETCORE_ENVIRONMENT=Production` 등 환경 변수 설정 후 실행. 상세는 [documentation/backend/environment-configuration.md](../../documentation/backend/environment-configuration.md) 참고.
