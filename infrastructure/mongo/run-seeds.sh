#!/usr/bin/env bash
# MongoDB 시드 스크립트 실행 (호스트에서 mongosh 필요)
# 사용법:
#   ./run-seeds.sh
#   MONGO_URI="mongodb://user:pass@host:27017/factory_mes?authSource=admin" ./run-seeds.sh
#
# 파일명 순서: infrastructure/mongo/seeds/*.js 를 정렬하여 순차 실행

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SEEDS_DIR="${SEEDS_DIR:-$ROOT_DIR/seeds}"
MONGO_URI="${MONGO_URI:-mongodb://admin:admin123@127.0.0.1:27017/factory_mes?authSource=admin}"

if ! command -v mongosh >/dev/null 2>&1; then
  echo "mongosh 가 PATH에 없습니다. MongoDB Shell을 설치한 뒤 다시 실행하세요." >&2
  exit 1
fi

if [[ ! -d "$SEEDS_DIR" ]]; then
  echo "시드 디렉터리가 없습니다: $SEEDS_DIR" >&2
  exit 1
fi

mapfile -t files < <(find "$SEEDS_DIR" -maxdepth 1 -type f -name '*.js' | sort)
if [[ ${#files[@]} -eq 0 ]]; then
  echo "실행할 .js 시드가 없습니다: $SEEDS_DIR" >&2
  exit 0
fi

echo "MongoDB: $MONGO_URI"
for f in "${files[@]}"; do
  echo "Running: $f"
  mongosh "$MONGO_URI" --file "$f"
done

echo "All seed scripts finished."
