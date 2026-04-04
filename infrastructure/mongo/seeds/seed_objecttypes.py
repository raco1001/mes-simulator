#!/usr/bin/env python3
"""
ObjectType 스키마(JSON)를 MongoDB object_type_schemas 컬렉션에 시딩한다.
문서 형식은 백엔드 MongoObjectTypeSchemaRepository와 동일: _id, objectType, payloadJson.

사용법:
  cd infrastructure/mongo/seeds
  python seed_objecttypes.py
  python seed_objecttypes.py --mongo-uri mongodb://admin:admin123@localhost:27017/?authSource=admin --db factory_mes
"""
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path

from pymongo import MongoClient

SEEDS_DIR = Path(__file__).resolve().parent


def _build_payload(dto: dict) -> dict:
    """DTO JSON에 createdAt/updatedAt(aware datetime)을 넣어 payloadJson으로 저장."""
    now = datetime.now(timezone.utc)
    payload = {**dto, "createdAt": now, "updatedAt": now}
    return payload


def seed(mongo_uri: str, db_name: str) -> None:
    client = MongoClient(mongo_uri)
    col = client[db_name]["object_type_schemas"]

    files = sorted(SEEDS_DIR.glob("*_objecttype.json"))
    if not files:
        print("No *_objecttype.json files in", SEEDS_DIR)
        client.close()
        return

    for path in files:
        dto = json.loads(path.read_text(encoding="utf-8"))
        object_type = dto["objectType"]
        payload_json = _build_payload(dto)
        doc = {
            "_id": object_type,
            "objectType": object_type,
            "payloadJson": payload_json,
        }
        result = col.replace_one({"objectType": object_type}, doc, upsert=True)
        verb = "Upserted" if result.matched_count or result.upserted_id is not None else "Replaced"
        print(f"  [{verb}] {object_type}")

    client.close()
    print("Seeding complete.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Seed ObjectType schemas into MongoDB.")
    parser.add_argument(
        "--mongo-uri",
        default="mongodb://admin:admin123@127.0.0.1:27017/?authSource=admin",
        help="MongoDB connection URI",
    )
    parser.add_argument(
        "--db",
        default="factory_mes",
        help="Database name",
    )
    args = parser.parse_args()
    seed(args.mongo_uri, args.db)
