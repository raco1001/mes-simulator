# Pipeline

Python 기반 데이터 파이프라인 서비스입니다. Kafka 이벤트를 처리하고 MongoDB에 분석/추천 결과를 반영합니다.  
Python data pipeline service that consumes Kafka events and writes analytics/recommendation outputs to MongoDB.

## 요구 사항 / Requirements

- Python 3.12+
- [uv](https://docs.astral.sh/uv/) (권장 / recommended)

## 설치 및 개발 환경 / Setup

```bash
uv venv
source .venv/bin/activate
uv pip install -e ".[dev]"
```

재현 설치(lock 기반) / Reproducible install:

```bash
uv venv
source .venv/bin/activate
uv pip sync requirements.lock
uv pip install -e ".[dev]"
```

## 실행 / Run

워크커 실행 / Worker:

```bash
uv run python -m workers.asset_worker
```

헬스체크 / Health check:

```bash
uv run pipeline-health
```

## 테스트 / Test

```bash
uv run pytest tests -v
```

## 현재 역할 / Current Role

- Kafka asset/simulation 관련 이벤트 소비
- MongoDB 상태/이벤트 기반 분석 보조
- 추천(Recommendation) 생성 및 피드백 루프 지원(phase 14/15 맥락)

## 계약 연동 / Contract Alignment

- API: `../../shared/api-schemas/openapi.json`
- Events: `../../shared/event-schemas/`
- Ontology: `../../shared/ontology-schemas/`

파이프라인 입력/출력 스키마는 shared contract와 함께 검토해야 합니다.  
Pipeline input/output assumptions should be validated against shared contracts.

## 트러블슈팅 / Troubleshooting

- Kafka 연결 실패: bootstrap server 및 topic 이름 확인
- Mongo write 실패: URL/auth/db 이름(`factory_mes`) 확인
- 처리 지연: consumer group lag와 브로커 상태 확인

## 참고 문서 / References

- `structure.md`
- `../../documentation/pipeline/README.md`
- `../../documentation/planning/to-be-phases/phase-14-pipeline-analytics.md`
- `../../documentation/planning/to-be-phases/phase-15-whatif-actions.md`
