# Factory MES

공장 운영 의사결정을 지원하는 온톨로지 기반 시뮬레이션 플랫폼입니다.  
Ontology-driven simulation platform for operational decision support in manufacturing scenarios.

## 현재 상태 / Current Status

- 완료 / Completed: Phase 10, 14, 15 (ontology metamodel, pipeline analytics/recommendations, What-if + action loop)
- 진행중 / In Progress: Phase 21, 22, 23 (simulation governance, UX hardening, multi-trigger semantics)
- 계약 기준 / Contract source of truth:
  - HTTP API: `shared/api-schemas/openapi.json`
  - Ontology schema: `shared/ontology-schemas/`
  - Event schema: `shared/event-schemas/`

## 컨셉과 용도 / Concept

- **Object/Asset**: 타입 스키마와 메타데이터로 정의되는 운영 객체.  
  Operational object defined by type schema and metadata.
- **State**: 동적 key-value 기반 런타임 상태.  
  Dynamic key-value runtime state.
- **Simulation**: 관계 전파 + 속성별 behavior 전략(`constant`, `settable`, `rate`, `accumulator`, `derived`).  
  Relationship propagation with behavior-specific simulation strategies.
- **Decision loop**: 이벤트 수집 -> 분석/추천 -> What-if 검증 -> 적용/피드백.  
  Event ingest -> analytics/recommendation -> What-if validation -> apply/feedback.

## 시스템을 먼저 사용해 보기 / Quick Start

### 1) 로컬 개발 실행 / Run locally (Backend + Frontend)

MongoDB/Kafka 인프라가 준비된 상태를 가정합니다.  
Assumes MongoDB/Kafka infrastructure is already available.

```bash
# terminal 1: backend (http://localhost:5000)
cd servers/backend && dotnet run --project DotnetEngine/DotnetEngine.csproj

# terminal 2: frontend (http://localhost:5173)
cd servers/frontend && npm install && npm run dev
```

선택적으로 파이프라인 워커를 실행할 수 있습니다.  
Optionally run the pipeline worker:

```bash
cd servers/pipeline && uv run python -m workers.asset_worker
```

### 2) Docker 실행 / Run with Docker (infra -> app)

```bash
# infrastructure
cd docker/infra && docker compose up -d

# app stack
cd ../app && docker compose up -d --build
```

접속 / Endpoints:
- Frontend: http://localhost:5173
- Backend API + Swagger: http://localhost:5000
- Kafka UI: http://localhost:8080
- Mongo Express: http://localhost:8081

상세 옵션은 [docker/README.md](docker/README.md)에서 확인하세요.  
See [docker/README.md](docker/README.md) for full options and checks.

## 문서와 세부 설명 위치 / Documentation Map

| 대상 / Scope | 위치 / Path | 확인할 내용 / Contents |
| --- | --- | --- |
| 프론트엔드 / Frontend | [servers/frontend/README.md](servers/frontend/README.md) | Canvas UX, simulation panel, API sync workflow |
| 백엔드 / Backend | [servers/backend/README.md](servers/backend/README.md) | Hexagonal modules, simulation and What-if flow, tests |
| 파이프라인 / Pipeline | [servers/pipeline/README.md](servers/pipeline/README.md) | Kafka consume, analytics/recommendations API, troubleshooting |
| Docker | [docker/README.md](docker/README.md) | Infra/app compose lifecycle and smoke validation |
| MongoDB | [infrastructure/mongo/README.md](infrastructure/mongo/README.md) | Bootstrap scripts and connection info |
| MongoDB 모델 / Model | [infrastructure/mongo/MODEL.md](infrastructure/mongo/MODEL.md) | Collections, indexes, and constraints |
| API 스키마 / API schema | [shared/api-schemas/README.md](shared/api-schemas/README.md) | REST/OpenAPI contract usage |
| 이벤트 스키마 / Event schema | [shared/event-schemas/README.md](shared/event-schemas/README.md) | Kafka topics and event payload contracts |
| 로드맵 / Roadmap | [documentation/planning/to-be-phases/00-overview.md](documentation/planning/to-be-phases/00-overview.md) | to-be phase dependency and status |

## 디렉터리 구조 요약 / Structure

```text
Scenario4/
├── servers/
│   ├── backend/      # ASP.NET Core + Mongo/Kafka + simulation engine
│   ├── frontend/     # React + TypeScript + Vite canvas client
│   └── pipeline/     # Python analytics/recommendation processing
├── shared/
│   ├── api-schemas/
│   ├── ontology-schemas/
│   └── event-schemas/
├── infrastructure/
│   └── mongo/
├── docker/
│   ├── infra/
│   └── app/
└── documentation/
    └── planning/to-be-phases/
```

## 핵심 Phase 문서 / Key Phase References

- [phase-10-ontology-metamodel.md](documentation/planning/to-be-phases/phase-10-ontology-metamodel.md)
- [phase-14-pipeline-analytics.md](documentation/planning/to-be-phases/phase-14-pipeline-analytics.md)
- [phase-15-whatif-actions.md](documentation/planning/to-be-phases/phase-15-whatif-actions.md)
- [phase-21-simulation-governance.md](documentation/planning/to-be-phases/phase-21-simulation-governance.md)
- [phase-22-UX.md](documentation/planning/to-be-phases/phase-22-UX.md)
- [phase-23-multi-trigger.md](documentation/planning/to-be-phases/phase-23-multi-trigger.md)
