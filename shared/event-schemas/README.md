# Event Schemas

이벤트 정보 형식의 규약을 정의한 JSON Schema 파일 모음입니다.

## 구조

```
event-schemas/
├── schemas/              # 개별 이벤트 스키마 정의
│   ├── asset.created.json
│   ├── asset.health.updated.json
│   └── alert.generated.json
├── topics/              # Kafka 토픽 정의
│   └── topics.json
├── versions/            # 스키마 버전 관리
│   └── v1.json
└── README.md
```

## 이벤트 스키마

### 1. asset.created

새로운 asset이 생성되었을 때 발생하는 이벤트입니다.

**예시:**
```json
{
  "eventType": "asset.created",
  "assetId": "freezer-1",
  "timestamp": "2026-02-18T10:00:00Z",
  "payload": {
    "type": "freezer",
    "connections": ["conveyor-1"],
    "metadata": {
      "location": "factory-floor-a",
      "capacity": "1000L"
    }
  }
}
```

### 2. asset.health.updated

Asset의 health 상태가 업데이트되었을 때 발생하는 이벤트입니다.

**예시:**
```json
{
  "eventType": "asset.health.updated",
  "assetId": "freezer-1",
  "timestamp": "2026-02-18T10:00:00Z",
  "payload": {
    "temperature": -5,
    "power": 120,
    "status": "normal"
  }
}
```

### 3. alert.generated

Asset에서 알림이 생성되었을 때 발생하는 이벤트입니다.

**예시:**
```json
{
  "eventType": "alert.generated",
  "assetId": "freezer-1",
  "timestamp": "2026-02-18T10:00:00Z",
  "payload": {
    "severity": "warning",
    "message": "Temperature above threshold",
    "threshold": -10,
    "current": -5,
    "metric": "temperature",
    "code": "TEMP_HIGH"
  }
}
```

## Kafka 토픽

토픽 이름 규칙: `{domain}.{entity}.{event}`

### 정의된 토픽

1. **factory.asset.events**
   - 모든 asset 관련 이벤트를 수집하는 통합 토픽
   - 이벤트: `asset.created`, `asset.health.updated`, `alert.generated`

2. **factory.asset.health**
   - Asset health 상태 업데이트 전용 토픽
   - 이벤트: `asset.health.updated`

3. **factory.asset.alert**
   - Asset 알림 전용 토픽
   - 이벤트: `alert.generated`

## 버전 관리

현재 버전: **v1** (2026-02-19 출시)

- MVP 초기 버전
- 3개 핵심 이벤트 스키마 정의 완료

## 사용 방법

### 스키마 검증

각 스키마 파일은 JSON Schema Draft 07 형식을 따릅니다. 스키마 검증을 위해서는 JSON Schema 검증 라이브러리를 사용할 수 있습니다.

**Python 예시:**
```python
import json
from jsonschema import validate, ValidationError

with open('schemas/asset.created.json') as f:
    schema = json.load(f)

event = {
    "eventType": "asset.created",
    "assetId": "freezer-1",
    "timestamp": "2026-02-18T10:00:00Z",
    "payload": {
        "type": "freezer",
        "connections": ["conveyor-1"],
        "metadata": {}
    }
}

try:
    validate(instance=event, schema=schema)
    print("Valid event")
except ValidationError as e:
    print(f"Invalid event: {e.message}")
```

**C# 예시:**
```csharp
using Newtonsoft.Json.Schema;

var schema = JSchema.Parse(File.ReadAllText("schemas/asset.created.json"));
var event = JObject.Parse(eventJson);

bool isValid = event.IsValid(schema);
```

## 확장 가이드

새로운 이벤트 타입을 추가할 때:

1. `schemas/` 디렉토리에 새 스키마 파일 생성 (예: `asset.deleted.json`)
2. `topics/topics.json`에 해당 이벤트를 포함하는 토픽 추가 또는 기존 토픽에 추가
3. `versions/v1.json` (또는 새 버전 파일)에 스키마 경로 추가
4. 이 README에 문서화

## 참고

- 모든 타임스탬프는 ISO 8601 형식 (`YYYY-MM-DDTHH:mm:ssZ`)을 사용합니다.
- `assetId`는 시스템 전역에서 고유해야 합니다.
- `payload`의 구조는 이벤트 타입에 따라 다릅니다.
- `metadata` 필드는 선택사항이며 자유 형식의 추가 정보를 담을 수 있습니다.
