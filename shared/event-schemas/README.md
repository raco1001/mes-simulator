# Event Schemas

이벤트 정보 형식의 규약을 정의한 JSON Schema 파일 모음입니다.

## 구조

```
event-schemas/
├── schemas/              # 개별 이벤트 스키마 정의
│   ├── event-envelope.json   # 공통 엔벨로프 (모든 이벤트가 $ref)
│   ├── asset.created.json
│   ├── asset.health.updated.json
│   ├── alert.generated.json
│   └── simulation.state.updated.json
├── topics/              # Kafka 토픽 정의
│   └── topics.json
├── versions/            # 스키마 버전 관리
│   └── v1.json
└── README.md
```

## 이벤트 스키마

모든 이벤트는 공통 엔벨로프(`event-envelope.json`)를 따릅니다. 필수 필드: `eventType`, `assetId`, `timestamp`, `schemaVersion`, `payload`.

- `schemaVersion`은 `^v\\d+$` 패턴을 따릅니다 (예: `v1`, `v2`).
- 시뮬레이션 컨텍스트에서는 선택적으로 `runId`가 포함될 수 있습니다.
- 운영 메타데이터는 선택적 `context` 객체(`additionalProperties: true`)에 담아 공식 페이로드 계약과 분리합니다.

### 1. asset.created

새로운 asset이 생성되었을 때 발생하는 이벤트입니다.

**예시:**
```json
{
  "eventType": "asset.created",
  "assetId": "freezer-1",
  "timestamp": "2026-02-18T10:00:00Z",
  "schemaVersion": "v1",
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
  "schemaVersion": "v1",
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
  "schemaVersion": "v1",
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

### 4. simulation.state.updated

시뮬레이션 전파 과정에서 상태가 갱신될 때 발생하는 이벤트입니다.
`payload`는 두 변형을 가집니다:
- 노드 업데이트: `tick`, `depth`, `status`, `temperature`, `power`
- 관계 전파: `tick`, `depth`, `relationshipType`, `fromAssetId`, `relationshipId?`

**예시 (관계 전파):**
```json
{
  "eventType": "simulation.state.updated",
  "assetId": "conveyor-1",
  "timestamp": "2026-02-18T10:00:02Z",
  "schemaVersion": "v1",
  "runId": "run-123",
  "payload": {
    "tick": 1,
    "depth": 1,
    "relationshipType": "ConnectedTo",
    "fromAssetId": "freezer-1",
    "relationshipId": "rel-1"
  }
}
```

## Kafka 토픽

토픽 이름 규칙: `{domain}.{entity}.{event}`

### 정의된 토픽

1. **factory.asset.events**
   - 모든 asset 관련 이벤트를 수집하는 실사용 단일 통합 토픽
   - 이벤트: `asset.created`, `asset.health.updated`, `simulation.state.updated`, `alert.generated`
   - (참고) 과거 분리 토픽(`factory.asset.health`, `factory.asset.alert`)은 현재 코드에서 사용하지 않으며 계약에서 제외됨.

## 버전 관리

현재 버전: **v1** (2026-02-19 출시)

- MVP 초기 버전
- 4개 핵심 이벤트 스키마 정의 완료

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
    "schemaVersion": "v1",
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
- `context`는 선택 필드이며 `traceId`, `correlationId`, `sourceService` 같은 운영 추적 정보를 담습니다.
