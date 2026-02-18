# dotnet-engine 환경 설정

WSL Ubuntu(개발/테스트)와 Windows(배포/스모크/운영)에서 경로·환경 차이 없이 쓰기 위한 설정 요약입니다.

---

## 가정

| 환경 | 용도 |
|------|------|
| **WSL Ubuntu** | 개발, 단위/통합 테스트 |
| **Windows** | 배포, 스모크 테스트, 운영 |

설정은 **appsettings + 환경 변수**만 사용하며, OS별 경로를 config에 넣지 않습니다.  
파일 경로가 필요하면 코드에서는 `IWebHostEnvironment.ContentRootPath`와 `Path.Combine`을 사용하고, 설정값은 환경 변수로 덮어쓰는 방식을 권장합니다.

---

## 설정 파일 구조

설정 파일은 **DotnetEngine/** (앱 프로젝트 루트) 아래에 있습니다.

| 파일 | 적용 시점 |
|------|-----------|
| `DotnetEngine/appsettings.json` | 공통 기본값 (로깅, AllowedHosts, Application.Name) |
| `DotnetEngine/appsettings.Development.json` | `ASPNETCORE_ENVIRONMENT=Development` 일 때 (WSL 로컬 실행) |
| `DotnetEngine/appsettings.Production.json` | `ASPNETCORE_ENVIRONMENT=Production` 일 때 (Windows 운영) |

나중에 로드된 설정이 이전 값을 덮어씁니다.  
환경 변수는 이보다 우선합니다 (예: `Application__Name`).

---

## WSL Ubuntu (개발·테스트)

- **환경**: `ASPNETCORE_ENVIRONMENT=Development` (기본 또는 `launchSettings.json` 사용 시 자동).
- **실행**: `dotnet run --project DotnetEngine/DotnetEngine.csproj` 또는 IDE F5 → `DotnetEngine/Properties/launchSettings.json`의 `dotnet-engine` 프로파일 사용.
- **URL**: `http://localhost:5000` (launchSettings에 고정).
- **로깅**: Debug 수준 (appsettings.Development.json).

별도로 `.env`나 경로 설정은 두지 않아도 됩니다.

---

## Windows (배포·스모크·운영)

### 1. 환경 변수 (필수)

배포/운영 시에는 반드시 다음을 설정합니다.

| 변수 | 권장값 | 설명 |
|------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 운영; 스모크는 `Staging` 등으로 구분 가능 |
| `ASPNETCORE_URLS` | (선택) | 지정하지 않으면 기본 `http://localhost:5000` |

### 2. 설정값 덮어쓰기 (선택)

appsettings를 바꾸지 않고 값만 바꾸려면 환경 변수로 덮어씁니다.

| 설정 키 (appsettings) | 환경 변수 (이중 언더스코어) |
|------------------------|-----------------------------|
| `Application:Name` | `Application__Name` |

예 (PowerShell, 스모크/운영 전; 퍼블리시 출력 폴더에서 실행):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Application__Name = "dotnet-engine-prod"   # 선택
dotnet DotnetEngine.dll
```

### 3. 경로를 쓰는 기능을 나중에 넣을 때

- config에는 **경로를 넣지 않고**, 환경 변수 한 개(예: `DATA_PATH`)로만 받습니다.
- 코드에서는 `Environment.GetEnvironmentVariable("DATA_PATH")` 또는 `IConfiguration["Data:Path"]`를 읽고, `Path.Combine`으로 조합합니다.  
  그러면 Linux(절대경로/상대경로)와 Windows 모두 동일한 방식으로 동작합니다.

---

## launchSettings.json (로컬 실행)

`DotnetEngine/Properties/launchSettings.json`은 **로컬 개발용**입니다.

- `dotnet run --project DotnetEngine/DotnetEngine.csproj` 또는 IDE에서 실행할 때 사용됩니다.
- **배포된 Windows 환경에서는 사용되지 않습니다.**  
  Windows에서는 위의 환경 변수로만 제어합니다.

---

## 요약

- **WSL**: `dotnet run`만 하면 Development + appsettings.Development.json 적용.
- **Windows**: `ASPNETCORE_ENVIRONMENT=Production` (및 필요 시 URL·Application 이름) 설정 후 `dotnet DotnetEngine.dll` 실행 (퍼블리시 출력 폴더 기준).
- **경로**: config에 OS별 경로를 두지 않고, 환경 변수 + `Path.Combine` / `IWebHostEnvironment`로 처리.
