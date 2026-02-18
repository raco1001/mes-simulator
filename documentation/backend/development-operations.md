# dotnet-engine 개발·운영 가이드

C# / ASP.NET Core 프로젝트(dotnet-engine)를 개발할 때 필요한 빌드, 실행, 테스트, 환경별 런타임 등 운영 지식을 정리한 문서입니다.

---

## 1. 빌드 (Build)

소스코드를 컴파일해 실행 파일(.dll 등)을 만드는 단계입니다.

### 기본 명령

프로젝트 루트(`dotnet-engine` 디렉터리)에서 실행합니다.

```bash
# 메인 프로젝트만 빌드
dotnet build DotnetEngine/DotnetEngine.csproj

# 테스트 프로젝트만 빌드
dotnet build DotnetEngine.Tests/DotnetEngine.Tests.csproj

# 솔루션 전체(메인 + 테스트) 빌드
dotnet build dotnet-engine.sln
```

### 빌드 결과 위치

| 구성        | 출력 경로 |
|------------|-----------|
| Debug      | `DotnetEngine/bin/Debug/net10.0/` |
| Release    | `DotnetEngine/bin/Release/net10.0/` |

### 자주 쓰는 옵션

| 옵션 | 설명 |
|------|------|
| `-c Release` | Release 구성으로 빌드(배포용). |
| `--no-restore` | 이미 restore된 상태에서 빌드만 수행. |

예:

```bash
dotnet build DotnetEngine/DotnetEngine.csproj -c Release
dotnet build dotnet-engine.sln --no-restore
```

---

## 2. 실행 (런타임)

### 로컬 개발(디버그) 실행

```bash
cd dotnet-engine
dotnet run --project DotnetEngine/DotnetEngine.csproj
```

- 기본적으로 **Debug** 구성, **Development** 환경으로 실행됩니다.
- 콘솔에 표시되는 URL(예: `http://localhost:5000`)로 브라우저나 `curl`로 접속합니다.
- Health 엔드포인트 예: `GET http://localhost:5000/api/health`

### 이미 빌드된 DLL만 실행

```bash
dotnet DotnetEngine/bin/Debug/net10.0/DotnetEngine.dll
```

### 환경·포트 변경

- **환경**: `ASPNETCORE_ENVIRONMENT` 환경 변수로 Development / Staging / Production 등 지정.
- **포트**: `DotnetEngine/Properties/launchSettings.json` 또는 `DotnetEngine/appsettings.json`·환경 변수로 설정.

---

## 3. 디버그 콘솔과 디버깅

### IDE에서 디버깅

- **F5**로 디버그 실행 시 `launch.json` 설정을 사용합니다.
- 중단점, 변수 조사, 디버그 콘솔 로그를 사용할 수 있습니다.

### 콘솔 출력

- `dotnet run`으로 실행한 **해당 터미널**이 곧 콘솔 출력 위치입니다.
- `Console.WriteLine`, 로거 출력, 예외 스택 등이 여기에 출력됩니다.

### 이 프로젝트에서

- ASP.NET Core 웹 서버가 떠 있으며, 요청이 올 때마다 로그가 콘솔에 출력됩니다.
- IDE의 **디버그 콘솔** 또는 **터미널** 탭에서 확인하면 됩니다.

---

## 4. 테스트 실행

### 전체 테스트 (권장)

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj
```

- restore → build → test를 한 번에 수행합니다.

### 빌드 생략하고 테스트만

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj --no-build
```

### 특정 테스트만 실행 (필터)

```bash
dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj --filter "FullyQualifiedName~HealthReport"
```

### 결과 확인

- 터미널에 Passed/Failed 개수와 실패 시 스택 트레이스가 출력됩니다.
- IDE의 **테스트 탐색기**에서 결과를 확인할 수 있습니다.

---

## 5. 개발 / 테스트 / 운영 런타임 구분

| 구분 | 용도 |
|------|------|
| **Development** | 로컬에서 `dotnet run` 할 때. 상세 에러 페이지, 개발용 설정. |
| **Testing** | 통합 테스트에서 `WebApplicationFactory`로 호스트를 띄울 때(테스트 코드에서 `UseEnvironment("Testing")` 사용). |
| **Production** | Windows Server 등 실제 서버에서 실행할 때. 에러 메시지 축소, 성능·보안 설정 적용. |

### 환경 지정

- **실행 시**: `ASPNETCORE_ENVIRONMENT=Production dotnet run ...`
- **Windows 서버**: IIS나 서비스로 실행할 때 해당 프로세스의 환경 변수에 `Production` 설정.

### 설정 파일

| 파일 | 적용 시점 |
|------|-----------|
| `appsettings.json` | 공통 기본값 |
| `appsettings.Development.json` | Development 환경 시 추가 적용 |
| `appsettings.Production.json` | Production 환경 시 추가 적용 |

환경에 따라 자동으로 덮어써 적용됩니다.

---

## 6. 자주 쓰는 명령 요약

| 목적 | 명령 |
|------|------|
| 패키지 복원 | `dotnet restore` |
| 솔루션 빌드 | `dotnet build dotnet-engine.sln` |
| 로컬 실행(개발) | `dotnet run --project DotnetEngine/DotnetEngine.csproj` |
| 테스트 | `dotnet test DotnetEngine.Tests/DotnetEngine.Tests.csproj` |
| 배포용 퍼블리시 | `dotnet publish DotnetEngine/DotnetEngine.csproj -c Release -o ./publish` |

### 배포용 퍼블리시 예시

```bash
dotnet publish DotnetEngine/DotnetEngine.csproj -c Release -o ./publish
```

- `publish` 폴더에 런타임에 필요한 파일만 모입니다.
- 이 폴더를 Windows 서버로 복사해 배포합니다.

---

## 7. Windows 서버 배포 시 참고

| 단계 | 내용 |
|------|------|
| **퍼블리시** | `dotnet publish -c Release -o ./publish` 후 `publish` 폴더를 서버로 복사. |
| **실행** | 서버에 .NET 10 런타임(또는 SDK) 설치 후 `dotnet DotnetEngine.dll` 실행. (퍼블리시 출력 폴더 기준) |
| **호스팅** | IIS 또는 Windows Service로 호스팅 가능. |
| **환경** | `ASPNETCORE_ENVIRONMENT=Production`(또는 호스팅 방식에 맞는 설정) 권장. |

---

## 8. 프로젝트 경로 기준 (Scenario4)

문서의 명령은 다음 기준 경로를 전제로 합니다.

```text
Scenario4/
└── dotnet-engine/          # 솔루션 루트, 여기서 dotnet 명령 실행
    ├── DotnetEngine/       # 앱 프로젝트 (진입점 + 설정)
    │   └── DotnetEngine.csproj
    ├── DotnetEngine.Tests/
    └── dotnet-engine.sln
```

다른 위치에서 실행할 때는 `--project` 또는 경로를 해당 위치에 맞게 바꿉니다.
