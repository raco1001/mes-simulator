# Docker Compose

`docker/infra`와 `docker/app`을 분리해 인프라와 애플리케이션을 단계적으로 운영합니다.  
Infrastructure and application stacks are split into `docker/infra` and `docker/app`.

## 구성 / Compose Scopes

| Scope | Path | Purpose |
| --- | --- | --- |
| Infra | `docker/infra/docker-compose.yml` | Kafka, Zookeeper, MongoDB, Kafka UI, Mongo Express |
| App | `docker/app/docker-compose.yml` | Backend, Frontend, Pipeline (requires existing `factory-network`) |

## 기동 순서 / Startup Order

### 1) Infra 먼저 기동 / Start infra first

```bash
cd docker/infra
docker compose up -d
```

### 2) App 기동 / Start app stack

```bash
cd ../app
docker compose up -d --build
```

### 3) 확인 / Verify endpoints

- Frontend: http://localhost:5173
- Backend: http://localhost:5000/api/health
- Kafka UI: http://localhost:8080
- Mongo Express: http://localhost:8081

## 통합 검증 루틴 / Smoke Validation Routine

```bash
curl -f http://localhost:5000/api/health
curl -f http://localhost:5000/api/assets
curl -f http://localhost:5000/api/states
curl -f http://localhost:5000/api/simulation/runs
```

추천/분석 흐름을 검증하려면 시뮬레이션 실행 후 Kafka/Mongo 로그를 함께 확인합니다.  
For analytics/recommendation validation, run simulation traffic and inspect Kafka/Mongo traces.

## 종료 / Shutdown

앱만 종료 / Stop app only:

```bash
cd docker/app
docker compose down
```

인프라까지 종료 / Stop infra too:

```bash
cd ../infra
docker compose down
```

## 운영 메모 / Operational Notes

- `docker/app`은 외부 네트워크 `factory-network`를 사용하므로 `docker/infra`를 먼저 올려야 합니다.
- Backend Mongo URL: `mongodb://admin:admin123@mongodb:27017/factory_mes?authSource=admin`
- Backend/Pipeline Kafka broker: `kafka:9093`
- 토픽 기본값 / Default topics:
  - `factory.asset.events`
  - `factory.asset.alert`

## 트러블슈팅 / Troubleshooting

- `network factory-network not found` -> infra compose를 먼저 실행
- backend unhealthy -> `docker logs mes-backend` 후 Mongo/Kafka 연결 확인
- pipeline idle -> topic 유입 이벤트 여부와 consumer group 상태 확인
