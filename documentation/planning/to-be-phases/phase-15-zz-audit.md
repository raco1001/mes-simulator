Phase 15 Audit — Frontend 구조 정리 및 실시간 스트림

Step 1: 설정 탭 제거 + 네비게이션 정리

목표: 홈 캔버스에서 이미 에셋/관계 CRUD를 제공하므로, 중복되는 설정 페이지를 제거한다.

변경 파일:

[servers/frontend/src/app/routes.tsx](servers/frontend/src/app/routes.tsx) — /settings 라우트 제거

[servers/frontend/src/app/layout/AppLayout.tsx](servers/frontend/src/app/layout/AppLayout.tsx) — 네비게이션에서 "설정" NavLink 제거

[servers/frontend/src/app/layout/AppLayout.test.tsx](servers/frontend/src/app/layout/AppLayout.test.tsx) — 설정 탭 관련 테스트 제거

[servers/frontend/src/test/utils.tsx](servers/frontend/src/test/utils.tsx) — SettingsPage import/라우트 제거

servers/frontend/src/pages/settings/ 디렉토리 자체는 삭제하거나 남겨둠 (코드 참고용).

Step 2: 시뮬레이션 위젯을 홈 사이드 패널로 통합

목표: 별도 /simulation 페이지에 있는 시뮬레이션 실행 기능을 홈 캔버스의 사이드 패널로 이동하여 에셋 그래프를 보면서 시뮬레이션을 제어할 수 있게 한다.

UX: 기존 관계 편집/에셋 편집과 동일한 사이드 패널 패턴. 툴바에 "시뮬레이션" 버튼을 추가하여 토글.

구현:

[servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx) 에 SimulationPanel 컴포넌트를 추가. 기존 [servers/frontend/src/pages/simulation/ui/SimulationPage.tsx](servers/frontend/src/pages/simulation/ui/SimulationPage.tsx) 의 로직(1회 실행, 지속 실행, 중단, 이벤트 보기)을 패널 형태로 재구성.

툴바에 "시뮬레이션" 토글 버튼 추가 (relMode/assetPanel과 상호 배타)

트리거 에셋 선택: 현재 선택된 노드를 자동 반영하거나 드롭다운에서 선택

AssetsCanvasPage.css — SimulationPanel 스타일 추가

라우팅:

[servers/frontend/src/app/routes.tsx](servers/frontend/src/app/routes.tsx) — /simulation 라우트 제거

[servers/frontend/src/app/layout/AppLayout.tsx](servers/frontend/src/app/layout/AppLayout.tsx) — "시뮬레이션" NavLink 제거 (홈에서 접근 가능하므로)

테스트 파일 업데이트

Step 3: 실시간 시뮬레이션 상태 SSE 스트림

목표: 지속 시뮬레이션 실행 중 tick마다 변경된 에셋 상태를 실시간으로 프론트엔드에 전달하여 캔버스 노드에 반영한다.

기존 Alert SSE 패턴(SseAlertChannel + AlertController.StreamAlerts)을 그대로 복제하여 시뮬레이션 이벤트 스트림을 구현한다.

flowchart LR
Engine["SimulationEngineService\n(BackgroundService)"] -->|"tick마다\nNotifyAsync()"| Channel["SseSimulationChannel\n(Channel per subscriber)"]
Channel -->|"IAsyncEnumerable"| Controller["SimulationController\n/api/simulation/stream"]
Controller -->|"SSE text/event-stream"| Frontend["Frontend\nEventSource"]
Frontend -->|"onmessage"| Canvas["AssetsCanvasPage\n노드 상태 갱신"]

3-1. Backend — SSE 채널 + 엔드포인트

신규 파일:

Infrastructure/Simulation/SseSimulationChannel.cs — ISimulationNotifier 구현, SseAlertChannel과 동일한 Channel 패턴

Application/Simulation/Ports/Driven/ISimulationNotifier.cs — Port 인터페이스

수정 파일:

[SimulationController.cs](servers/backend/DotnetEngine/Presentation/Controllers/SimulationController.cs) — GET /api/simulation/stream SSE 엔드포인트 추가

[SimulationEngineService.cs](servers/backend/DotnetEngine/Application/Simulation/Workers/SimulationEngineService.cs) — tick 처리 후 ISimulationNotifier.NotifyAsync() 호출. 이벤트 페이로드: { runId, tick, assetId, properties, status, timestamp }

[Program.cs](servers/backend/DotnetEngine/Program.cs) — SseSimulationChannel 싱글톤 DI 등록

SSE 이벤트 페이로드 (기존 StateDto 호환):

{
"runId": "run-123",
"tick": 5,
"assetId": "freezer-1",
"properties": { "currentTemp": -15.2, "inputPower": 800 },
"status": "normal",
"timestamp": "2026-03-31T..."
}

3-2. Frontend — SSE 구독 + 캔버스 반영

신규 파일:

entities/simulation/api/simulationStream.ts — subscribeSimulationEvents(runId, onEvent) 함수. alertStream.ts와 동일한 EventSource 패턴.

수정 파일:

[AssetsCanvasPage.tsx](servers/frontend/src/pages/canvas/ui/AssetsCanvasPage.tsx) — SimulationPanel에서 지속 실행 시작 후 subscribeSimulationEvents 구독 시작. 수신한 이벤트로 노드 데이터(asset.metadata / state) 및 시각적 상태(status 색상) 실시간 갱신.

[AssetNode.tsx](servers/frontend/src/pages/canvas/ui/AssetNode.tsx) — status 기반 시각적 표시 (normal=기본, warning=노란 테두리, error=빨간 테두리)

AssetsCanvasPage.css — 상태별 노드 색상 + 깜박임 애니메이션

Why this fits current scale

SSE는 Alert에서 이미 검증된 패턴이므로 동일 구조를 복제. 추가 인프라(WebSocket 서버, Redis pub/sub 등) 불요.

SimulationEngineService가 이미 1초 tick 루프를 실행하므로, 알림 호출 1줄 추가만으로 스트림 연동 가능.

프론트엔드 변경은 로컬 state만 사용하여 별도 상태관리 라이브러리 불요.

If scale increases

SSE → WebSocket 전환으로 양방향 통신 (실시간 파라미터 변경 등)

여러 Run 동시 구독을 위한 topic 기반 필터링

캔버스 노드 수가 100+ 되면 상태 갱신 debounce/throttle 필요

---

## Step 4: ObjectTypeSchema / LinkTypeSchema — payloadJson 타입을 string에서 BsonDocument로 전환

**목표**: `payloadJson` 필드 이름은 유지하되, 값을 JSON 문자열로 직렬화하는 대신 MongoDB BSON 서브도큐먼트로 직접 저장하도록 전환한다. `System.Text.Json` 수동 직렬화 코드를 제거하고 MongoDB driver가 변환을 담당하게 한다.

### 현재 구조 (문제)

```
{
  "_id": "Contains",
  "linkType": "Contains",
  "payloadJson": "{\"schemaVersion\":\"v1\",\"linkType\":\"Contains\",...}"  ← string
}
```

- `payloadJson` 값이 JSON 문자열로 이중 인코딩되어 mongosh에서 가독성이 낮음
- Repository에서 `JsonSerializer.Serialize / Deserialize`를 수동으로 관리해야 함
- `payloadJson` 내부 필드(`direction`, `properties` 등)를 MongoDB validator나 인덱스로 활용 불가

### 변경 후 구조 (목표)

```
{
  "_id": "Contains",
  "linkType": "Contains",
  "payloadJson": {                ← BsonDocument (서브도큐먼트)
    "schemaVersion": "v1",
    "linkType": "Contains",
    "displayName": "포함",
    "direction": "Hierarchical",
    "properties": [],
    ...
  }
}
```

### 변경 파일

**1. `MongoObjectTypeSchemaDocument.cs` / `MongoLinkTypeSchemaDocument.cs`**

`PayloadJson` 필드 타입을 `string` → `BsonDocument`로 변경.

```csharp
// 변경 전
[BsonElement("payloadJson")]
public string PayloadJson { get; set; } = "{}";

// 변경 후
[BsonElement("payloadJson")]
public BsonDocument PayloadJson { get; set; } = new BsonDocument();
```

**2. `MongoObjectTypeSchemaRepository.cs` / `MongoLinkTypeSchemaRepository.cs`**

`ToDocument`: `JsonSerializer.Serialize(dto)` 대신 `dto.ToBsonDocument()` 사용.

`ToDto`: `JsonSerializer.Deserialize(doc.PayloadJson)` 대신 `BsonSerializer.Deserialize<T>(doc.PayloadJson)` 사용.

```csharp
// ToDocument — 변경 후
private static MongoObjectTypeSchemaDocument ToDocument(ObjectTypeSchemaDto dto) =>
    new()
    {
        ObjectType = dto.ObjectType,
        ObjectTypeValue = dto.ObjectType,
        PayloadJson = dto.ToBsonDocument(),   // BsonDocument로 직접 변환
    };

// ToDto — 변경 후
private static ObjectTypeSchemaDto? ToDto(MongoObjectTypeSchemaDocument doc)
{
    try { return BsonSerializer.Deserialize<ObjectTypeSchemaDto>(doc.PayloadJson); }
    catch { return null; }
}
```

Enum 직렬화 주의: MongoDB driver는 enum을 기본적으로 정수로 직렬화하므로, `Program.cs`에서 `ConventionPack`을 통해 전역 설정이 필요하다.

```csharp
// Program.cs — MongoClient 등록 이전에 추가
var conventionPack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };
ConventionRegistry.Register("EnumAsString", conventionPack, _ => true);
```

이 설정으로 `"Directed"`, `"Number"`, `"Settable"` 등 enum 값이 문자열로 저장된다.

**3. `init-collections.js`**

Seed 데이터에서 `payloadJson: JSON.stringify({...})` 형태를 `payloadJson: {...}` 객체 직접 삽입 형태로 변경.

```js
// 변경 전
{ _id: 'Contains', linkType: 'Contains', payloadJson: JSON.stringify({ schemaVersion: 'v1', ... }) }

// 변경 후
{ _id: 'Contains', linkType: 'Contains', payloadJson: { schemaVersion: 'v1', linkType: 'Contains', ... } }
```

`$jsonSchema` validator의 `payloadJson` 타입도 `string` → `object`로 수정.

### 마이그레이션

기존 데이터의 `payloadJson`은 문자열이므로 새 구조와 호환되지 않는다. 스키마 컬렉션만 재초기화하면 충분하다.

```bash
# mongosh 내에서 (object_type_schemas, link_type_schemas만 재초기화)
db.object_type_schemas.drop()
db.link_type_schemas.drop()
# 이후 init-collections.js 재실행
```

또는 전체 볼륨 초기화:

```bash
cd docker/infra && docker compose down -v && docker compose up -d
```

### Why this fits current scale

- `payloadJson` 이름은 유지되므로 문서 구조와 DB 컬렉션 validator의 변경 범위가 최소화됨.
- MongoDB driver가 BSON 직렬화를 담당하므로 Repository에서 `System.Text.Json` 의존성이 제거됨.
- 기존 `MongoRelationshipDocument`의 `BsonDocument Properties` 패턴과 일관성이 맞춰짐.

### If scale increases

- `payloadJson.direction`, `payloadJson.properties` 등 서브도큐먼트 내부 필드로 MongoDB 인덱스 추가 가능.
- BSON 네이티브 저장이 되면 MongoDB Atlas Search 연동도 가능.

---

## Step 5: 열거값 케이싱 통일 (PascalCase)

**배경**: Step 4에서 `JsonStringEnumConverter`를 추가한 결과, C# 백엔드는 열거값을 PascalCase 문자열로 출력한다(`"Number"`, `"Directed"` 등). 그러나 공유 스키마와 seed 데이터는 여전히 lowercase를 사용하여 불일치가 생겼다.

**변경 대상:**

- [`shared/ontology-schemas/ontology-common-defs.json`](shared/ontology-schemas/ontology-common-defs.json) — 모든 enum 값 PascalCase로 변경
  - `DataType`: `"number"` → `"Number"` 등
  - `SimulationBehavior`: `"constant"` → `"Constant"` 등
  - `Mutability`: `"immutable"` → `"Immutable"`, `"mutable"` → `"Mutable"`
  - `TraitPersistence/Dynamism/Cardinality`: 동일하게 PascalCase
  - `LinkDirection`: `"directed"` → `"Directed"` 등
  - `LinkTemporality`: `"event_driven"` → `"EventDriven"` (snake_case 제거)
- `shared/ontology-schemas/examples/` 전체 (freezer, battery, conveyor, invalid/)
  - `dataType`, `simulationBehavior`, `mutability`, `traits.*` 값 → PascalCase
- [`infrastructure/mongo/init-scripts/init-collections.js`](infrastructure/mongo/init-scripts/init-collections.js)
  - `object_type_schemas` seed의 `payloadJson.properties[].dataType` 등 → PascalCase
  - `link_type_schemas` seed의 `payloadJson.direction`, `payloadJson.temporality` → PascalCase
  - `$jsonSchema` validator 내 enum 값 → PascalCase

**마이그레이션**: 기존 컬렉션 seed 데이터는 볼륨 재초기화 또는 `db.collection.updateMany()`로 갱신 필요.

### Why this fits current scale

공유 JSON 스키마는 문서 역할이며, 실제 런타임 검증은 MongoDB validator와 C# DTO가 담당한다. 케이싱을 통일하면 세 레이어(스키마, 백엔드, 프론트엔드)가 동일한 값을 사용하여 디버깅 비용이 줄어든다.

### If scale increases

열거값 목록이 늘어나면 `ontology-common-defs.json`만 수정하면 C# enum, 프론트엔드 타입, MongoDB validator가 공통 소스를 공유하는 구조이므로 변경 범위가 최소화된다.

---

## Step 6: 도량형 스키마 정의

**목표**: `PropertyDefinition.unit` 필드를 임의 문자열에서 관리된 단위 참조로 강화한다. 기본 단위(Base)와 파생 단위(Derived)를 구분하는 독립 스키마 파일을 추가하고, 어떤 속성에 단위가 필요한지 명시한다.

**신규 파일:**

`shared/ontology-schemas/unit-definitions.json`

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://shadow-boxing.local/schemas/unit-definitions.json",
  "$defs": {
    "BaseUnit": {
      "enum": ["m", "kg", "s", "A", "K", "C", "W", "Wh", "L", "item"]
    },
    "DerivedUnit": {
      "enum": ["%", "mps", "kW", "kWh", "itemsPerHour", "degC", "degF"]
    },
    "Unit": {
      "oneOf": [
        { "$ref": "#/$defs/BaseUnit" },
        { "$ref": "#/$defs/DerivedUnit" }
      ]
    }
  }
}
```

**수정 파일:**

- [`shared/ontology-schemas/property-definition.json`](shared/ontology-schemas/property-definition.json)
  - `unit` 필드를 자유 `string`에서 `$ref: unit-definitions.json#/$defs/Unit`으로 변경
- `shared/ontology-schemas/examples/` 예시 파일들
  - `"unit": "mps"`, `"unit": "Wh"`, `"unit": "C"` → 정규화된 값으로 교체

**단위가 필요한 속성 식별 기준**: `dataType: "Number"`이고 물리적 의미를 가진 속성. 순수 비율(`%`)과 카운트(`item`)도 포함.

### Why this fits current scale

단위 정의를 독립 파일로 분리하면 `property-definition.json`은 `$ref`만 가지므로 단위 목록 변경이 한 곳에 집중된다. 런타임에는 단위가 메타데이터로만 사용되므로 추가 인프라 불필요.

### If scale increases

Units.NET 라이브러리 도입 시 `BaseUnit` enum 값을 Units.NET 식별자와 1:1 매핑하면 자동 단위 변환(kW ↔ W)이 가능해진다. 이기종 단위를 가진 에셋 간 `Supplies` 관계에서 시뮬레이션 엔진이 자동 변환을 수행할 수 있다.

---

## Step 7: ObjectTypeSchema 인터페이스 상속 구조

**목표**: 공통 속성 집합을 가진 ObjectTypeSchema를 "인터페이스 스키마"로 정의하고, 구체 스키마가 `extends`로 이를 참조해 속성을 상속받는 구조를 도입한다. 인터페이스 속성(base)과 구체 스키마 추가 속성(own)을 키로 구분한다.

**스키마 개념 설계:**

```
InterfaceObjectTypeSchema  (abstractSchema: true)
  예: "electrical-equipment"
  ownProperties: [{ key: "nominalPower", ... }, { key: "operatingVoltage", ... }]

ConcreteObjectTypeSchema
  예: "freezer"
  extends: "electrical-equipment"
  ownProperties: [{ key: "targetTemp", ... }]
  → resolvedProperties = parent.ownProperties + child.ownProperties  (런타임 결합)
```

**수정 파일:**

- [`shared/ontology-schemas/object-type-schema.json`](shared/ontology-schemas/object-type-schema.json)
  - `extends?: string` 필드 추가 (부모 objectType ID 참조)
  - `abstractSchema?: boolean` 필드 추가
  - 기존 `properties` → `ownProperties`로 rename

- 백엔드 `ObjectTypeSchemaDto.cs`
  - `Extends?: string`, `AbstractSchema: bool`, `OwnProperties` 추가
  - `GetObjectTypeSchemaQueryHandler` — 조회 시 부모 스키마를 fetch하여 `OwnProperties` 머지 후 `ResolvedProperties` 반환

- 프론트엔드 `object-type-schema/model/types.ts`
  - `extends?: string`, `abstractSchema?: boolean`, `ownProperties: PropertyDefinition[]` 추가

**상속 해결(resolve) 로직 위치:**

백엔드 쿼리 핸들러에서 처리. 클라이언트는 항상 머지된 결과를 받는다.

```
flowchart LR
  Client -->|"GET /api/object-type-schemas/freezer"| Handler
  Handler -->|"freezer.extends = electrical-equipment"| ParentFetch["fetch parent schema"]
  ParentFetch --> Merge["parent.ownProperties + child.ownProperties"]
  Merge -->|"resolvedProperties"| Client
```

### Why this fits current scale

`extends`와 `ownProperties`를 추가 필드로 도입하므로 기존 스키마(extends 없음)는 영향을 받지 않는다. 해결 로직이 쿼리 핸들러 한 곳에 집중되어 복잡도가 낮게 유지된다.

### If scale increases

다단계 상속(`A extends B extends C`)이 필요하면 핸들러를 재귀 fetch로 확장한다. 순환 참조 방지를 위해 등록 시 유효성 검사 추가가 필요하다.
