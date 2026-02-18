# Health Check 모듈

## 1. 목적

- **현 시점**: Kafka/Redis 등 인프라 없이 **프로젝트 초기화 상태만 확인**.
- **역할**: 애플리케이션이 기동 가능한지, 설정이 로드되는지 확인하는 최소 Health Check.

## 2. 규약 (도메인·DTO·Value Object)

### 2.1 도메인 (domains/health)

- **HealthStatusKind**  
  - 상수: `HEALTHY`, `DEGRADED`, `UNHEALTHY` (문자열).

- **HealthReport** (Value Object)  
  - `status`, `description`, `application_name`, `reported_at`.  
  - 빈 `application_name` → `HealthConstants.Defaults.ApplicationName`.  
  - 빈 `status` → `Unhealthy`.

- **HealthConstants**  
  - `Status.Healthy/Degraded/Unhealthy`, `Defaults.ApplicationName`.

### 2.2 DTO (pipelines/health_dto.py)

- **HealthStatusDto** (Pydantic)  
  - `status`, `description`, `application_name`, `reported_at` (datetime, JSON 직렬화 가능).

### 2.3 Use Case (pipelines/health_pipeline.py)

- **get_health(settings?: Settings) -> HealthStatusDto**  
  - 설정에서 `application_name` 사용, 현재 시각(UTC)으로 `HealthReport` 생성 후 DTO 반환.  
  - 현재는 항상 `healthy` 반환 (인프라 검사 없음).

## 3. 테스트 (테스트 주도)

- **domains/health**: `HealthStatusKind` 상수, `HealthReport` 생성·기본값 동작.
- **pipelines**: `HealthStatusDto` 스키마·직렬화, `get_health()` 반환 타입·값·설정 반영.
- **workers**: `main()` stdout JSON 필드, exit code 0 (healthy 시).

## 4. 구현 요약

- `workers/health_worker.py`: `get_health()` 호출 → JSON 출력 → status가 healthy면 exit 0, 아니면 exit 1.
- CLI 진입점: `pipeline-health` (pyproject.toml `[project.scripts]`).

## 5. 실행 검증

```bash
pipeline-health
```

- 기대: JSON 출력 (`status`, `description`, `application_name`, `reported_at`), exit code 0.

예시 출력:

```json
{
  "status": "healthy",
  "description": "Application is running.",
  "application_name": "pipeline",
  "reported_at": "2026-02-18T07:20:51.731577Z"
}
```
