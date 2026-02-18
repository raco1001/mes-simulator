# dotnet-engine 구조 (Layered + Hexagonal)

```
dotnet-engine/                    # 솔루션 루트
├── DotnetEngine/                 # 앱 프로젝트 (진입점 + 비즈니스 로직)
│   ├── Domain/
│   │   └── Health/
│   │       ├── Constants/        # HealthConstants
│   │       └── ValueObjects/     # HealthStatusKind, HealthReport
│   ├── Application/
│   │   └── Health/
│   │       ├── Dto/              # HealthStatusDto
│   │       ├── Ports/            # IGetHealthQuery (primary port)
│   │       └── Handlers/         # GetHealthQueryHandler (use case)
│   ├── Presentation/
│   │   └── Controllers/          # HealthController (primary adapter)
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   ├── Program.cs
│   └── DotnetEngine.csproj
├── DotnetEngine.Tests/
│   ├── Domain/Health/...
│   ├── Application/Health/...
│   └── Presentation/...
├── dotnet-engine.sln
└── README.md
```

- **Port**: `IGetHealthQuery` — 애플리케이션이 제공하는 진입점.
- **Adapter**: `HealthController` — HTTP로 Port를 호출.
- **Domain**: 엔티티/Value Object/상수는 Domain 하위에만 두고, 인프라/프레젠테이션에 의존하지 않음.
- **설정**: appsettings, Properties는 DotnetEngine/ 안에 두어 실행 시 Content Root에서 로드.
