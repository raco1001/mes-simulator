#!/bin/sh
# Docker Mongo 초기화 시점에만 실행 (볼륨 최초 1회).
# /mongo-seeds/*.js 를 파일명 순으로 적용. compose에서 seeds → /mongo-seeds 마운트 필요.

set -eu

SEEDS_DIR="${SEEDS_DIR:-/mongo-seeds}"
URI="mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@127.0.0.1:27017/factory_mes?authSource=admin"

if [ ! -d "$SEEDS_DIR" ]; then
  echo "zz-run-seeds: skip (no directory: $SEEDS_DIR)" >&2
  exit 0
fi

found=0
for f in $(find "$SEEDS_DIR" -maxdepth 1 -type f -name '*.js' | sort); do
  found=1
  echo "zz-run-seeds: running $f"
  mongosh "$URI" --file "$f"
done

if [ "$found" -eq 0 ]; then
  echo "zz-run-seeds: no .js files in $SEEDS_DIR" >&2
fi

echo "zz-run-seeds: done"
