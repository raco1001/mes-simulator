# Factory MES

공장 설비(Asset)와 상태(State)를 관리하고 시뮬레이션 하기 위한 **Factory MES(Manufacturing Execution System)** 시나리오 프로젝트입니다.  
에셋 메타데이터·현재 상태의 CRUD, 이벤트 기반 파이프라인(Kafka → Data Pipeline -> MongoDB)을 포함하며, 추후 시뮬레이션·알림 등으로 확장할 수 있는 구조입니다.

---

## 컨셉과 용도

- **에셋(Asset)**: 설비 단위(예: freezer, conveyor, sensor). 타입·연결·메타데이터로 정의.
- **상태(State)**: 에셋별 현재 값(온도, 전력, 상태 등). 이벤트 소비로 갱신.
- **흐름**: 프론트엔드/API로 에셋·상태 조회·생성·수정 → MongoDB 저장. (선택) Kafka 이벤트를 파이프라인이 소비해 상태·이벤트 로그 저장.
- **확장**: 로그인, 시뮬레이션 실행, 알림 등은 라우트·API·이벤트 스키마를 추가하는 방식으로 확장 가능.

---

## 시스템을 먼저 사용해 보기

### 1) 로컬에서 백엔드 + 프론트만 실행 (MongoDB 로컬/원격)

백엔드가 사용할 MongoDB가 이미 떠 있다고 가정합니다.

```bash
# 터미널 1: 백엔드 (기본 http://localhost:5000)
cd servers/backend && dotnet run --project DotnetEngine/DotnetEngine.csproj

# 터미널 2: 프론트엔드 (기본 http://localhost:5173)
cd servers/frontend && pnpm install && pnpm dev
```

브라우저에서 **http://localhost:5173** 접속 후, **메인**에서 에셋 목록·상태 확인, **에셋 설정**에서 에셋 생성(Type, Connections 입력) 및 시뮬레이션 버튼(UI만)을 사용할 수 있습니다.

- 백엔드 연결 문자열·포트: [servers/backend](servers/backend) 및 [documentation/backend/environment-configuration.md](documentation/backend/environment-configuration.md) 참고.
- MongoDB가 없으면 아래 Docker 인프라를 먼저 띄운 뒤, 백엔드 연결 설정을 맞추면 됩니다.

### 2) Docker로 인프라 + 앱 전체 실행

인프라(Kafka, MongoDB, Redis 등)와 앱(Backend, Frontend)을 Docker Compose로 한 번에 띄우는 방법입니다.

1. **인프라 기동**  
   [docker/README.md](docker/README.md) 참고.
   ```bash
   cd docker && docker compose -f docker-compose.infra.yml up -d
   ```
2. **앱 기동**  
   동일 문서의 “앱만 빌드·기동” 절차 참고.
   ```bash
   cd docker && docker compose -f docker-compose.app.yml build && docker compose -f docker-compose.app.yml up -d
   ```
3. **접속**
   - Frontend: http://localhost:5173
   - Backend API / Swagger: http://localhost:5000

상세 옵션·네트워크·통합 테스트 스크립트는 [docker/README.md](docker/README.md)에 정리되어 있습니다.

---

## 문서와 세부 설명이 있는 위치

각 서비스·인프라별 상세(설치, 실행, 테스트, 구조)는 아래 문서에서 확인하면 됩니다.

| 대상                 | 위치                                                                 | 확인할 내용                                                           |
| -------------------- | -------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **프론트엔드**       | [servers/frontend/README.md](servers/frontend/README.md)             | 기술 스택, 개발 서버, 빌드·테스트, 프로젝트 구조, API 엔드포인트 요약 |
| **백엔드**           | [servers/backend/README.md](servers/backend/README.md)               | 실행·테스트, 모듈(Health/Asset) 구성, Port/Adapter/Use Case           |
| **백엔드 상세 문서** | [documentation/backend/README.md](documentation/backend/README.md)   | 개발·운영, 환경 설정, Asset Command/Query 기획·확장                   |
| **파이프라인**       | [servers/pipeline/README.md](servers/pipeline/README.md)             | Python 환경, 설치·테스트, Health Check 실행                           |
| **파이프라인 문서**  | [documentation/pipeline/README.md](documentation/pipeline/README.md) | 프로젝트 설정, Health Check 모듈                                      |
| **Docker**           | [docker/README.md](docker/README.md)                                 | compose 파일 용도, 인프라/앱 기동 순서, 통합 테스트                   |
| **MongoDB**          | [infrastructure/mongo/README.md](infrastructure/mongo/README.md)     | 빠른 시작, 컬렉션·인덱스 초기화, 접속 정보                            |
| **MongoDB 모델**     | [infrastructure/mongo/MODEL.md](infrastructure/mongo/MODEL.md)       | DB·컬렉션 구조, 스키마, 인덱스, 제약                                  |
| **API 스키마**       | [shared/api-schemas/README.md](shared/api-schemas/README.md)         | REST API 스키마(assets/state), DTO 예시                               |
| **이벤트 스키마**    | [shared/event-schemas/README.md](shared/event-schemas/README.md)     | 이벤트 타입, Kafka 토픽, 버전·확장 가이드                             |

---

## 디렉터리 구조 요약

```
mes-system/
├── servers/
│   ├── frontend/     # React + Vite, 에셋 목록·에셋 설정·시뮬레이션(UI)
│   ├── backend/      # ASP.NET Core, Asset/State API, MongoDB 어댑터
│   └── pipeline/     # Python, Kafka 소비 → MongoDB/Redis 등
├── infrastructure/
│   └── mongo/        # MongoDB 초기화 스크립트, 모델 문서
├── shared/
│   ├── api-schemas/  # REST API 스키마(JSON)
│   └── event-schemas/# 이벤트 스키마, 토픽 정의
├── docker/           # Compose(인프라/앱), 통합 테스트 스크립트
└── documentation/    # 백엔드·파이프라인 상세 문서, ADR 등
```

API 규약은 백엔드 컨트롤러와 [shared/api-schemas](shared/api-schemas)를 함께 보면 됩니다.  
이벤트·토픽 규약은 [shared/event-schemas](shared/event-schemas)와 [infrastructure/mongo/MODEL.md](infrastructure/mongo/MODEL.md)을 참고하면 됩니다.
