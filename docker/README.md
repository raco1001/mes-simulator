# Docker Compose

## 구성

| 파일 | 용도 |
|------|------|
| `docker-compose.infra.yml` | 인프라만 (Kafka, MongoDB, Redis, UI) |
| `docker-compose.app.yml` | 앱만 (Backend, Frontend) — **기동 중인 인프라 네트워크 사용** |
| `docker-compose.full.yml` | 인프라 + 앱 전체 (앱 서비스는 별도 정의 시 사용) |

## 통합 테스트 (인프라 이미 기동 중일 때)

1. **인프라가 이미 떠 있는지 확인**
   ```bash
   docker network ls | grep factory-network
   ```
   없으면:
   ```bash
   cd docker && docker compose -f docker-compose.infra.yml up -d
   ```

2. **앱만 빌드·기동 후 통합 테스트**
   ```bash
   cd docker
   chmod +x scripts/integration-test.sh
   ./scripts/integration-test.sh
   ```
   또는 수동:
   ```bash
   cd docker
   docker compose -f docker-compose.app.yml build
   docker compose -f docker-compose.app.yml up -d
   curl -s http://localhost:5000/api/health
   curl -s http://localhost:5000/api/assets
   curl -s http://localhost:5000/api/states
   ```

3. **브라우저 확인**
   - Frontend: http://localhost:5173
   - Backend API: http://localhost:5000 (Swagger: http://localhost:5000 루트)

4. **앱만 종료**
   ```bash
   cd docker && docker compose -f docker-compose.app.yml down
   ```

## 주의

- `docker-compose.app.yml`은 **외부 네트워크** `factory-network`를 사용합니다.  
  인프라를 먼저 `docker-compose.infra.yml`로 띄워 두어야 합니다.
- MongoDB 연결 문자열은 컨테이너 내부에서 호스트명 `mongodb`로 접속합니다.
