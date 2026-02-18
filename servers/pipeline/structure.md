# pipeline 구조 (Clean Architecture)

```
pipeline/
│
├── src/
│   ├── config/           # 설정 (pydantic-settings)
│   │   ├── __init__.py
│   │   └── settings.py
│   ├── domains/          # 도메인: 엔티티, Value Object, 상수
│   │   ├── __init__.py
│   │   └── health/
│   │       ├── __init__.py
│   │       ├── constants.py
│   │       └── value_objects.py
│   ├── pipelines/        # Use Case: 파이프라인/핸들러
│   │   ├── __init__.py
│   │   ├── health_dto.py
│   │   └── health_pipeline.py
│   ├── messaging/
│   │   ├── consumer/      # Kafka consumer (Primary Adapter, 추후)
│   │   └── provider/      # Kafka producer (추후)
│   ├── repositories/     # Port + Adapter (MongoDB, Redis, Postgres 추후)
│   ├── workers/          # 진입점: consumer_worker, health_worker
│   │   ├── __init__.py
│   │   └── health_worker.py
│   ├── shared/           # 공통 예외, 로깅, 스키마
│   │   ├── __init__.py
│   │   └── exceptions.py
│   └── ...
│
├── scripts/              # CLI / 배치 실행
├── tests/
│   ├── conftest.py
│   ├── domains/health/
│   ├── pipelines/
│   └── workers/
├── docker/
├── pyproject.toml
├── docker-compose.yml
└── README.md
```

- **Domain**: `domains/health` — HealthStatusKind, HealthReport (VO), HealthConstants.
- **Application (Use Case)**: `pipelines/health_pipeline.py` — `get_health()` → HealthStatusDto.
- **Primary Adapter**: `workers/health_worker.py` — CLI 진입점 (`pipeline-health`).
- **의존성**: Domain ← Pipelines ← Workers; 설정은 Config에서 주입.
