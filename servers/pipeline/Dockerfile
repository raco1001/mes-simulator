# Pipeline (Python) - Dockerfile
# Build context: servers/pipeline
# 설정: src/config/settings.py (PIPELINE_* env), pyproject.toml (entry: pipeline-asset-worker)

FROM python:3.12-slim AS runtime

WORKDIR /app

# 소스 및 메타 복사 (setuptools가 src/ 내 패키지 탐색)
COPY pyproject.toml ./
COPY src/ ./src/
# pyproject readme 필드용 (없으면 pip install 시 메타데이터 오류 가능)
RUN touch README.md
RUN pip install --no-cache-dir --upgrade pip setuptools wheel \
    && pip install --no-cache-dir .

# 설정은 환경변수로 주입 (PIPELINE_KAFKA_BOOTSTRAP_SERVERS, PIPELINE_MONGODB_URL 등)
# 기본값은 settings.py와 동일; compose에서 오버라이드
ENV PIPELINE_APPLICATION_NAME=pipeline
ENV PIPELINE_KAFKA_TOPIC_ASSET_EVENTS=factory.asset.events
ENV PIPELINE_KAFKA_CONSUMER_GROUP_ID=pipeline-asset-consumer
ENV PIPELINE_MONGODB_DATABASE=factory_mes

# pyproject.toml [project.scripts] → pipeline-asset-worker
ENTRYPOINT ["pipeline-asset-worker"]
