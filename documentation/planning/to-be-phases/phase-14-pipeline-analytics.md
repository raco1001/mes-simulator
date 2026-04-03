# Phase 14 — Pipeline 피벗: 분석 + Recommendation (Layer 4a)

**신규 Phase. Python Pipeline을 Kafka 소비자에서 분석·추천 엔진으로 확장한다.**

---

## 1. 목표

1. **FastAPI 도입**: Pipeline 서비스에 HTTP API 계층 추가 (health, metrics, recommendation 조회)
2. **분석 계층**: Pandas 기반 트렌드 감지 파이프라인 구축
3. **Recommendation 도메인**: 분석 결과를 기반으로 운영 추천을 생성하고, Kafka + MongoDB로 전달·저장

### 왜 이 Phase가 필요한가

기존 Pipeline은 "임계값 초과 → Alert 발행"만 수행한다. Palantir Foundry 스타일의 운영 의사결정 지원을 위해서는:

- 단순 임계값이 아닌 **트렌드 기반** 판정 (값이 줄어들고 있는가, 도달 예측 시간은?)
- Alert보다 구체적인 **Recommendation** 생성 ("배터리-1의 chargeRate를 높여야 2시간 내 방전됨")
- Recommendation을 조회하고 What-if 시뮬레이션으로 검증할 수 있는 **API**

---

## 2. 선행 조건

- Phase 12 완료 (SimulationBehavior 엔진 — 분석이 behavior 정보를 참조)
- Phase 13 완료 (LinkTypeSchema — 관계 흐름 규칙을 분석에 활용)

---

## 3. FastAPI 도입

### 3.1 서비스 구조 변경

```
servers/pipeline/
  src/
    main.py              # [NEW] FastAPI app + Kafka worker 통합 시작점
    api/                 # [NEW]
      health.py          # GET /health, GET /ready
      metrics.py         # GET /metrics/summary
      recommendations.py # GET /recommendations, GET /recommendations/{id}
    workers/
      asset_worker.py    # 기존 Kafka 소비자 (변경 최소화)
    pipelines/
      asset_pipeline.py  # 기존 (calculate_state 확장)
      analysis_pipeline.py  # [NEW] 트렌드 분석
      recommendation_pipeline.py  # [NEW] 추천 생성
    domains/
      asset/             # 기존
      recommendation/    # [NEW]
        value_objects.py
        rules.py
```

### 3.2 FastAPI + Kafka Worker 통합

```python
# main.py
from fastapi import FastAPI
from contextlib import asynccontextmanager

@asynccontextmanager
async def lifespan(app: FastAPI):
    worker_task = asyncio.create_task(start_kafka_worker())
    yield
    worker_task.cancel()

app = FastAPI(title="Pipeline Analytics", lifespan=lifespan)
```

- FastAPI 앱이 시작되면 Kafka Worker도 백그라운드 태스크로 실행
- 기존 `asset_worker.py` 로직은 거의 변경 없이 유지
- Dockerfile 변경: `uvicorn src.main:app` 으로 시작점 변경

### 3.3 API 엔드포인트

| Method | Path | 설명 |
| --- | --- | --- |
| `GET` | `/health` | liveness |
| `GET` | `/ready` | Kafka 연결 + MongoDB 연결 상태 |
| `GET` | `/metrics/summary` | 최근 N분 이벤트 처리량, 알림 수, 추천 수 |
| `GET` | `/recommendations` | 추천 목록 (필터: status, objectType, severity) |
| `GET` | `/recommendations/{id}` | 추천 상세 |
| `PATCH` | `/recommendations/{id}` | 상태 업데이트 (approved, rejected, applied) |

### 3.4 의존성 추가

```
# requirements.txt 추가분
fastapi>=0.100.0
uvicorn>=0.23.0
pandas>=2.0.0
```

---

## 4. 분석 계층 (analysis_pipeline.py)

### 4.1 목적

시뮬레이션 이벤트 스트림에서 트렌드를 감지하고, 임계치 도달 예측 시간을 계산한다.

### 4.2 분석 함수

**이동 평균 (Moving Average)**

```python
def moving_average(values: list[float], window: int = 5) -> float:
    """최근 N개 값의 이동 평균"""
    if not values:
        return 0.0
    return sum(values[-window:]) / min(len(values), window)
```

**선형 추세 (Linear Trend)**

```python
def linear_trend(timestamps: list[float], values: list[float]) -> tuple[float, float]:
    """단순 선형 회귀. (slope, intercept) 반환"""
    df = pd.DataFrame({"t": timestamps, "v": values})
    if len(df) < 2:
        return (0.0, values[-1] if values else 0.0)
    slope = df["v"].diff().mean() / df["t"].diff().mean()
    intercept = df["v"].iloc[-1] - slope * df["t"].iloc[-1]
    return (slope, intercept)
```

**임계치 도달 예측 (Time to Threshold)**

```python
def time_to_threshold(
    current: float, slope: float, threshold: float
) -> float | None:
    """현재 추세로 threshold에 도달하는 예상 시간(초). 도달 불가능이면 None."""
    if slope == 0:
        return None
    remaining = (threshold - current) / slope
    return remaining if remaining > 0 else None
```

### 4.3 트렌드 분석 흐름

```
이벤트 수신 (asset_worker)
  → 최근 이벤트 버퍼 (MongoDB 또는 메모리 윈도우)
  → analysis_pipeline.analyze(buffer)
    → moving_average, linear_trend, time_to_threshold
  → TrendResult { objectId, propertyKey, slope, predictedThresholdTime }
  → recommendation_pipeline.evaluate(trend_results)
```

---

## 5. Recommendation 도메인

### 5.1 Value Objects

```python
@dataclass(frozen=True)
class Recommendation:
    id: str
    object_id: str
    object_type: str
    severity: str            # "info" | "warning" | "critical"
    category: str            # "efficiency" | "safety" | "maintenance"
    title: str
    description: str
    suggested_action: dict   # { "property": "chargeRate", "targetValue": 500 }
    analysis_basis: dict     # { "trend_slope": -0.5, "predicted_depletion": 7200 }
    status: str              # "pending" | "approved" | "rejected" | "applied"
    created_at: datetime
    updated_at: datetime
```

### 5.2 추천 규칙 (rules.py)

규칙 기반 추천 생성. 각 규칙은 TrendResult를 입력받아 Recommendation을 출력(또는 None).

```python
class RecommendationRule(Protocol):
    def evaluate(self, trend: TrendResult, schema: ObjectTypeSchema) -> Recommendation | None:
        ...
```

**초기 규칙 예시**

| 규칙 | 조건 | 추천 |
| --- | --- | --- |
| `DepletionWarning` | accumulator 속성의 slope < 0이고 predicted depletion < 2시간 | "chargeRate를 높이거나 outflow를 줄이세요" |
| `OverheatWarning` | rate 속성의 slope > 0이고 predicted threshold < 30분 | "냉각 자원을 연결하거나 부하를 줄이세요" |
| `EfficiencyDrop` | derived 효율 속성이 이동평균 대비 10% 이상 하락 | "관련 설비 점검 또는 파라미터 조정 추천" |

### 5.3 추천 생성 파이프라인 (recommendation_pipeline.py)

```python
async def generate_recommendations(
    trends: list[TrendResult],
    schemas: dict[str, ObjectTypeSchema],
    rules: list[RecommendationRule],
) -> list[Recommendation]:
    recommendations = []
    for trend in trends:
        schema = schemas.get(trend.object_type)
        if not schema:
            continue
        for rule in rules:
            rec = rule.evaluate(trend, schema)
            if rec:
                recommendations.append(rec)
    return recommendations
```

### 5.4 저장 및 전달

- MongoDB `recommendations` 컬렉션에 저장
- Kafka `factory.recommendation.generated` 토픽으로 발행
- 프론트엔드가 Kafka 이벤트를 통해 실시간 알림 수신 (Phase 15에서 대시보드 구현)

---

## 6. calculate_state() 확장

기존 `calculate_state()`의 하드코딩 임계값 로직을 동적 properties 기반으로 변경:

**as-is**:
```python
if state.get("currentTemp", 0) > TEMP_THRESHOLD:
    return "overheat"
```

**to-be**:
```python
def calculate_state(properties: dict, schema: ObjectTypeSchema) -> str:
    for prop_def in schema.properties:
        value = properties.get(prop_def.key)
        if value is None:
            continue
        constraints = prop_def.constraints
        if constraints and constraints.get("max") is not None:
            if value > constraints["max"]:
                return f"{prop_def.key}_exceeded"
    return "normal"
```

---

## 7. 거버넌스

- Recommendation의 `category`는 열린 문자열 (새 카테고리 자유 추가)
- Recommendation의 `severity`는 닫힌 체계: `info`, `warning`, `critical`
- 추천 규칙 추가 시 `RecommendationRule` 프로토콜 구현 필수
- Kafka 토픽 네이밍: `factory.recommendation.*` 패턴 유지
- Pipeline의 분석 윈도우 크기, 임계값 등은 환경 변수로 설정 가능하게 구성

---

## 8. 테스트

| 테스트 | 검증 내용 |
| --- | --- |
| `test_moving_average` | 정상 계산, 빈 배열, 윈도우 > 배열 길이 |
| `test_linear_trend` | 상승/하강 추세, 데이터 부족 |
| `test_time_to_threshold` | 정상 예측, slope=0, 이미 초과 |
| `test_depletion_warning_rule` | accumulator 고갈 예측 시 추천 생성 |
| `test_overheat_warning_rule` | rate 속성 임계치 접근 시 추천 생성 |
| `test_efficiency_drop_rule` | 효율 하락 시 추천 생성 |
| `test_recommendation_pipeline` | 통합 흐름 (트렌드 → 규칙 → 추천 목록) |
| `test_calculate_state_dynamic` | 동적 properties 기반 상태 판정 |
| `test_fastapi_health` | /health 엔드포인트 |
| `test_fastapi_recommendations` | /recommendations 엔드포인트 |

---

## 9. 완료 기준

- [x] FastAPI 앱 구동 + Kafka Worker 백그라운드 실행
- [x] `/health`, `/ready`, `/metrics/summary` 엔드포인트 동작
- [x] `/recommendations` CRUD 엔드포인트 동작
- [x] `analysis_pipeline.py`의 3개 분석 함수 (이동 평균, 선형 추세, 임계치 예측)
- [x] `recommendation_pipeline.py`의 규칙 기반 추천 생성
- [x] 최소 3개 추천 규칙 구현 (DepletionWarning, OverheatWarning, EfficiencyDrop)
- [x] MongoDB `recommendations` 컬렉션 저장
- [x] Kafka `factory.recommendation.generated` 토픽 발행
- [x] `calculate_state()` 동적 properties 대응
- [x] 모든 테스트 통과

---

## 10. 산출물

| 산출물 | 변경 유형 |
| --- | --- |
| `src/main.py` (FastAPI 시작점) | 신규 |
| `src/api/health.py` | 신규 |
| `src/api/metrics.py` | 신규 |
| `src/api/recommendations.py` | 신규 |
| `src/pipelines/analysis_pipeline.py` | 신규 |
| `src/pipelines/recommendation_pipeline.py` | 신규 |
| `src/domains/recommendation/value_objects.py` | 신규 |
| `src/domains/recommendation/rules.py` | 신규 |
| `src/pipelines/asset_pipeline.py` (`calculate_state`) | 수정 |
| `src/workers/asset_worker.py` (분석 호출 연동) | 수정 |
| `Dockerfile` (uvicorn 시작) | 수정 |
| `requirements.txt` (FastAPI, Pandas) | 수정 |
| `docker-compose` (포트 매핑) | 수정 |
| `init-collections.js` (recommendations 컬렉션) | 수정 |
| 테스트 10건 | 신규/수정 |

---

## 11. 확장 시 변경 예상

| 규모 변화 | 현재 설계 | 확장 시 변경 |
| --- | --- | --- |
| 복잡한 분석 | Pandas 기반 단순 통계 | scikit-learn 이상 탐지, 시계열 예측 |
| 대량 이벤트 | 메모리 윈도우 | Redis Sorted Set 또는 TimescaleDB |
| 배치 분석 | 없음 | Celery 또는 Airflow 스케줄러 |
| 다중 추천 엔진 | 단일 규칙 파이프라인 | ML 기반 추천 + 규칙 기반 앙상블 |

---

## 12. 참고

- [Phase 12 — SimulationBehavior 엔진](phase-12-simulation-behavior.md) — behavior 정보 참조
- [Phase 13 — LinkTypeSchema + 전파](phase-13-link-schema-propagation.md) — 관계 흐름 규칙 참조
- [as-is Pipeline 구조](../../servers/pipeline/README.md)
- [ADR-20260218](../../documentation/ADR/ADR-20260218.md) — ML Pipeline 배치 결정

---

## Phase 11 계약 영향 노트

- `alert.generated` 이벤트 payload는 단일 metric 필드(`metric/current/threshold/code`)가 아니라 `metrics[]`를 사용한다.
- Phase 14 분석/추천 로직은 alert payload를 재사용할 경우 `payload.metrics[]`를 순회하는 방식으로 소비해야 한다.
