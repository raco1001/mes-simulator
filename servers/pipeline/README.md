# pipeline

Python 기반 데이터 파이프라인 (Kafka consume → MongoDB 저장, 추후 Redis/Postgres).

## 요구 사항

- Python 3.12+ (프로젝트별 버전: `.python-version` → pyenv 사용 시 해당 버전 자동 적용)
- [uv](https://docs.astral.sh/uv/) (권장), (선택) pyenv

## 설치 및 개발 환경

**uv 사용 (권장):**

```bash
# uv 설치 (최초 1회): curl -LsSf https://astral.sh/uv/install.sh | sh
cd pipeline
uv venv                              # .python-version 있으면 해당 Python으로 생성
source .venv/bin/activate             # Windows: .venv\Scripts\activate
uv pip install -e ".[dev]"
```

**재현 가능 설치 (lock 기준):**

```bash
uv venv && source .venv/bin/activate
uv pip sync requirements.lock         # requirements.lock 기준 정확히 설치
uv pip install -e ".[dev]"            # dev 의존성 + editable
```

## 테스트

```bash
pytest tests -v
# 또는 (가상환경 없이): uv run pytest tests -v
```

## Health Check 실행

```bash
pipeline-health
# 또는 (가상환경 없이): uv run pipeline-health
# 또는: python -m workers.health_worker
```

정상 시 JSON 출력 후 exit code 0.

## 구조

- [structure.md](structure.md) — 디렉터리 및 아키텍처 개요.
- [documentation/pipeline/](../documentation/pipeline/) — 설정, 모듈, 운영 문서.
