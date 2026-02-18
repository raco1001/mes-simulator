# pipeline 프로젝트 설정

## 1. 초기화 요약

- **목적**: Kafka 이벤트 consume → MongoDB 저장, 추후 Redis/Postgres 연동을 위한 Python 파이프라인 골격.
- **아키텍처**: Clean Architecture (Domain / Application Use Case / Adapters).
- **초기 범위**: 인프라(Kafka, Redis 등) 없이 **프로젝트 초기화 상태만 확인하는 Health Check** 모듈만 구현.

## 2. 프레임워크·라이브러리

| 용도 | 라이브러리 | 비고 |
|------|------------|------|
| 설정 | pydantic-settings | 타입 있는 설정, env/.env |
| 스키마·검증 | pydantic | DTO, 도메인 모델 |
| 테스트 | pytest | TDD, 단위/통합 |
| (선택) 품질 | pytest-cov, ruff | 커버리지, 린트 |

추후 예정: confluent-kafka, pymongo, redis, psycopg3 등.

## 3. 개발 환경 (pyenv + Python 3.12)

- **Python**: 3.12+ (프로젝트별 버전은 `.python-version`으로 고정)
- **패키지 관리**: pyproject.toml, `pip install -e ".[dev]"`

### 3.1 pyenv로 이 프로젝트만 최신 Python 사용

이 프로젝트 루트에 `.python-version`이 있으면, 해당 디렉터리에서만 지정한 버전이 사용됩니다.

```bash
# 1) pyenv로 3.12 설치 (최초 1회)
pyenv install 3.12

# 2) pipeline 디렉터리로 이동 (자동으로 3.12 적용)
cd /path/to/Scenario4/pipeline
pyenv version   # 예: 3.12.x (set by .../pipeline/.python-version)

# 3) 가상환경 생성 및 설치
python -m venv .venv
source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -e ".[dev]"
```

### 3.2 다른 Python 버전으로 바꾸기

- **3.13 사용**: `.python-version` 내용을 `3.13`으로 수정 후 `pyenv install 3.13` 실행.
- **3.14 사용**: `.python-version`을 `3.14`로 수정 후 `pyenv install 3.14` 실행.

```bash
# .python-version 수정 후
pyenv install 3.13   # 없을 때만
python -m venv .venv --clear && source .venv/bin/activate
pip install -e ".[dev]"
```

### 3.3 pyenv 없이 사용

시스템(또는 다른 방법)으로 Python 3.12 이상을 설치한 경우:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
```

## 4. 테스트 실행

```bash
pytest tests -v
# 또는
.venv/bin/python -m pytest tests -v
```

- 테스트는 `tests/` 하위, `conftest.py`에서 `src` 경로 보정.
- 패키지 설치 시 `pythonpath`는 pyproject.toml의 `[tool.pytest.ini_options]`에 `pythonpath = ["src"]`로 설정됨.

## 5. 프로젝트 구조 (요약)

- **domains/** — 도메인 Value Object, 상수 (의존성 없음).
- **pipelines/** — Use Case (get_health 등), DTO.
- **workers/** — CLI/진입점 (Primary Adapter).
- **config/** — Settings.
- **repositories/** — Port/Adapter (추후 MongoDB, Redis, Postgres).
- **messaging/** — Kafka consumer/provider (추후).

## 6. 네이밍 규칙

- **디렉터리·파일**: `snake_case`.
- **클래스·타입**: `CapWords`.
- **함수·변수**: `snake_case`.
- **테스트 파일**: `test_<module>.py`.

## 7. 디렉터리/프로젝트 이름 변경 후 재초기화

디렉터리명을 `data-pipeline` → `pipeline`으로 바꾸거나, pyproject.toml의 `name`을 바꾼 경우에는 다음을 진행하세요.

1. **기존 가상환경·빌드 산출물 제거**  
   - `.venv`는 이전 경로가 하드코딩되어 있으므로 삭제 후 다시 생성합니다.  
   - `src/*.egg-info`(예: `data_pipeline.egg-info`)가 있으면 삭제합니다.

2. **가상환경 재생성 및 설치**

```bash
cd /path/to/Scenario4/pipeline
rm -rf .venv src/*.egg-info
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
```

3. **동작 확인**

```bash
pytest tests -v
pipeline-health
```
