#!/usr/bin/env bash
# Docker Compose 통합 테스트 (Phase 15 E2E 포함)
# 전제: docker/infra 로 인프라가 이미 기동 중
# 사용: ./scripts/integration-test.sh

set -e

COMPOSE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$COMPOSE_DIR"

BACKEND_URL="${BACKEND_URL:-http://localhost:5000}"
PIPELINE_URL="${PIPELINE_URL:-http://localhost:8000}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:5173}"
PASS=0
FAIL=0

pass() { echo "  ✓ $1"; PASS=$((PASS+1)); }
fail() { echo "  ✗ $1"; FAIL=$((FAIL+1)); }
check() {
  local desc="$1" actual="$2" expected="$3"
  if echo "$actual" | grep -q "$expected"; then
    pass "$desc"
  else
    fail "$desc (expected '$expected', got '$actual')"
  fi
}

echo "=== Docker Compose 통합 테스트 ==="
echo ""

echo "--- 0. 인프라(infra) 기동 여부 확인 ---"
if ! docker network inspect factory-network &>/dev/null; then
  echo "오류: factory-network 없음. 먼저 인프라를 기동하세요:"
  echo "  cd docker/infra && docker compose up -d"
  exit 1
fi
pass "factory-network 존재"

echo ""
echo "--- 1. 서비스 헬스 체크 ---"
for i in $(seq 1 30); do
  if curl -sf "$BACKEND_URL/api/health" >/dev/null 2>&1; then
    pass "Backend 준비됨 ($BACKEND_URL)"
    break
  fi
  if [ "$i" -eq 30 ]; then
    fail "Backend가 30초 내에 응답하지 않음"
    exit 1
  fi
  sleep 1
done

for i in $(seq 1 20); do
  if curl -sf "$PIPELINE_URL/health" >/dev/null 2>&1; then
    pass "Pipeline 준비됨 ($PIPELINE_URL)"
    break
  fi
  if [ "$i" -eq 20 ]; then
    fail "Pipeline이 20초 내에 응답하지 않음"
  fi
  sleep 1
done

echo ""
echo "--- 2. 기본 API 검증 ---"
HTTP_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$BACKEND_URL/api/assets")
check "GET /api/assets" "$HTTP_CODE" "200"
HTTP_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$BACKEND_URL/api/states")
check "GET /api/states" "$HTTP_CODE" "200"
HTTP_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$BACKEND_URL/api/alerts")
check "GET /api/alerts" "$HTTP_CODE" "200"

echo ""
echo "--- 3. Phase 15 E2E: 추천 생성 확인 (Step 1: Seed) ---"
echo "  Pipeline 추천 목록 조회..."
REC_LIST=$(curl -sf "$PIPELINE_URL/recommendations" 2>/dev/null || echo "[]")
REC_COUNT=$(echo "$REC_LIST" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
if [ "$REC_COUNT" -gt 0 ]; then
  pass "Pipeline recommendations: $REC_COUNT 건"
  REC_ID=$(echo "$REC_LIST" | python3 -c "import sys,json; print(json.load(sys.stdin)[0].get('recommendationId',''))" 2>/dev/null)
  REC_STATUS=$(echo "$REC_LIST" | python3 -c "import sys,json; print(json.load(sys.stdin)[0].get('status',''))" 2>/dev/null)
  check "첫 추천 status" "$REC_STATUS" "pending"
else
  echo "  ⚠ 추천 없음 -- 시뮬레이션이 아직 실행되지 않았을 수 있음 (skip)"
  REC_ID=""
fi

echo ""
echo "--- 4. Phase 15 E2E: What-if dry-run (Step 2) ---"
if [ -n "$REC_ID" ]; then
  TRIGGER_ID=$(echo "$REC_LIST" | python3 -c "
import sys,json
rec=json.load(sys.stdin)[0]
sa=rec.get('suggestedAction',{})
print(sa.get('triggerAssetId', rec.get('objectId','')))" 2>/dev/null)

  STATE_BEFORE=$(curl -sf "$BACKEND_URL/api/states/$TRIGGER_ID" 2>/dev/null || echo "{}")

  WHATIF_RESP=$(curl -sf -X POST "$BACKEND_URL/api/simulation/what-if" \
    -H 'Content-Type: application/json' \
    -d "{\"triggerAssetId\":\"$TRIGGER_ID\",\"patch\":{\"properties\":{}},\"maxDepth\":3}" 2>/dev/null || echo "{}")

  check "What-if 응답에 runId 포함" "$WHATIF_RESP" "whatif-"
  check "What-if 응답에 deltas 포함" "$WHATIF_RESP" "deltas"

  STATE_AFTER=$(curl -sf "$BACKEND_URL/api/states/$TRIGGER_ID" 2>/dev/null || echo "{}")
  if [ "$STATE_BEFORE" = "$STATE_AFTER" ]; then
    pass "What-if dry-run: 상태 미변경 확인"
  else
    fail "What-if dry-run: 상태가 변경됨!"
  fi
else
  echo "  ⚠ 추천 없음 -- What-if 건너뜀"
fi

echo ""
echo "--- 5. Phase 15 E2E: Apply (Step 3) ---"
if [ -n "$REC_ID" ]; then
  APPLY_RESP=$(curl -sf -X POST "$BACKEND_URL/api/recommendations/$REC_ID/apply" 2>/dev/null || echo "{}")
  check "Apply 응답: success" "$APPLY_RESP" '"success":true'
  check "Apply 응답: runId 포함" "$APPLY_RESP" "runId"

  sleep 2

  REC_AFTER=$(curl -sf "$PIPELINE_URL/recommendations/$REC_ID" 2>/dev/null || echo "{}")
  check "Apply 후 status=applied" "$REC_AFTER" '"status":"applied"'
else
  echo "  ⚠ 추천 없음 -- Apply 건너뜀"
fi

echo ""
echo "--- 6. Phase 15 E2E: Outcome 확인 (Step 4) ---"
if [ -n "$REC_ID" ]; then
  sleep 3
  REC_OUTCOME=$(curl -sf "$PIPELINE_URL/recommendations/$REC_ID" 2>/dev/null || echo "{}")
  check "Outcome 필드 존재" "$REC_OUTCOME" "outcome"
  check "Outcome appliedRunId 존재" "$REC_OUTCOME" "appliedRunId"
else
  echo "  ⚠ 추천 없음 -- Outcome 건너뜀"
fi

echo ""
echo "--- 7. Frontend 접근성 확인 ---"
HTTP_CODE=$(curl -s -o /dev/null -w '%{http_code}' "$FRONTEND_URL" 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
  pass "Frontend 접속 가능 ($FRONTEND_URL)"
else
  fail "Frontend 접속 불가 (HTTP $HTTP_CODE)"
fi

echo ""
echo "==============================="
echo "  통합 테스트 결과: $PASS passed, $FAIL failed"
echo "==============================="
echo "  Backend:  $BACKEND_URL"
echo "  Pipeline: $PIPELINE_URL"
echo "  Frontend: $FRONTEND_URL"

[ "$FAIL" -eq 0 ] || exit 1
