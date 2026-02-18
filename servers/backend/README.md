# dotnet-engine

C# / ASP.NET Core 백엔드 (Windows Server 배포 예정).  
Layered + Hexagonal Architecture, Health Check 모듈 템플릿 적용.

## 실행

```bash
dotnet run --project DotnetEngine/DotnetEngine.csproj
# GET http://localhost:5000/api/health
```

## 테스트

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj
```

## Health 모듈 (템플릿)

- **Domain**: `HealthConstants`, `HealthStatusKind`, `HealthReport` (Value Object)
- **Port**: `IGetHealthQuery` (Application)
- **Adapter**: `HealthController` (Presentation) — `GET /api/health`
- **Use Case**: `GetHealthQueryHandler` — 도메인 HealthReport를 DTO로 반환

새 모듈 추가 시 동일하게 Port → 테스트 선행 → Handler/Adapter 구현 순서로 확장할 수 있음.

## 환경 설정

- **WSL(개발)**: `dotnet run` 시 `ASPNETCORE_ENVIRONMENT=Development`, `appsettings.Development.json` 적용. (`DotnetEngine/Properties/launchSettings.json` 참고.)
- **Windows(배포/운영)**: `ASPNETCORE_ENVIRONMENT=Production` 등 환경 변수 설정 후 실행. 상세는 `documentation/engine/environment-configuration.md` 참고.
