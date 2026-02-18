#!/usr/bin/env bash
# Docker Compose 통합 테스트
# 전제: docker-compose.infra.yml 로 인프라가 이미 기동 중
# 사용: ./scripts/integration-test.sh

set -e

COMPOSE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$COMPOSE_DIR"

echo "=== Docker Compose 통합 테스트 ==="
echo "1. 인프라(infra) 기동 여부 확인..."
if ! docker network inspect factory-network &>/dev/null; then
  echo "오류: factory-network 없음. 먼저 인프라를 기동하세요:"
  echo "  cd docker && docker compose -f docker-compose.infra.yml up -d"
  exit 1
fi
echo "   factory-network 확인됨."

echo ""
echo "2. App 서비스(backend, frontend) 빌드 및 기동..."
docker compose -f docker-compose.app.yml build --no-cache 2>/dev/null || docker compose -f docker-compose.app.yml build
docker compose -f docker-compose.app.yml up -d

echo ""
echo "3. Backend 헬스 체크 대기..."
for i in $(seq 1 30); do
  if curl -sf http://localhost:5000/api/health >/dev/null 2>&1; then
    echo "   Backend 준비됨."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "   오류: Backend가 30초 내에 응답하지 않음."
    docker compose -f docker-compose.app.yml logs backend
    exit 1
  fi
  sleep 1
done

echo ""
echo "4. API 통합 검증..."
echo "   GET /api/health"
curl -sf http://localhost:5000/api/health | head -c 200
echo ""
echo "   GET /api/assets"
curl -sf http://localhost:5000/api/assets
echo ""
echo "   GET /api/states"
curl -sf http://localhost:5000/api/states
echo ""

echo ""
echo "=== 통합 테스트 완료 ==="
echo "  Backend:  http://localhost:5000"
echo "  Frontend: http://localhost:5173"
echo "  (종료: cd docker && docker compose -f docker-compose.app.yml down)"
