# MES Simulator — 구현 계획서

> 에이전트가 이 문서를 읽고 바로 작업을 실행할 수 있도록 작성됨.
> 각 Task는 독립 실행 단위. 의존 관계는 "선행 Task" 항목으로 명시.

---

## Phase 1 — 파이프라인 온톨로지 기반 전환

### Task 1-A: property-definition.json 스키마 확장

**파일:** `shared/ontology-schemas/property-definition.json`

기존 `properties` 객체 내에 아래 두 필드를 추가한다.

```json
"alertThresholds": {
  "type": "array",
  "description": "속성 값 기반 경보 임계값 목록. 온톨로지 스키마에 정의되며 파이프라인이 런타임에 읽어 판단한다.",
  "items": {
    "type": "object",
    "required": ["level", "condition", "value"],
    "additionalProperties": false,
    "properties": {
      "level":     { "type": "string", "enum": ["warning", "error"] },
      "condition": { "type": "string", "enum": ["lt", "lte", "gt", "gte"] },
      "value":     { "type": "number" }
    }
  }
},
"derivedRule": {
  "type": "object",
  "description": "simulationBehavior=Derived 일 때만 유효. 선형 관계 기반 계산 규칙.",
  "required": ["type", "inputs"],
  "additionalProperties": false,
  "properties": {
    "type":     { "type": "string", "enum": ["linear"] },
    "timeUnit": { "type": "string", "enum": ["second", "minute", "hour"], "default": "second" },
    "inputs": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["property", "coefficient"],
        "properties": {
          "property":    { "type": "string", "description": "같은 Asset 내 참조할 속성 key" },
          "coefficient": { "type": "number", "description": "음수 = 소비, 양수 = 증가" }
        }
      }
    }
  }
}
```

---

### Task 1-B: Drone ObjectType seed 파일 생성

**파일 생성:** `infrastructure/mongo/seeds/drone_objecttype.json`

```json
{
  "schemaVersion": "v1",
  "objectType": "Drone",
  "displayName": "Drone",
  "traits": {
    "persistence": "Ephemeral",
    "dynamism": "Dynamic",
    "cardinality": "Multiple"
  },
  "ownProperties": [
    {
      "key": "battery_level",
      "dataType": "Number",
      "unit": "%",
      "simulationBehavior": "Derived",
      "mutability": "Mutable",
      "baseValue": 100,
      "constraints": { "min": 0, "max": 100 },
      "alertThresholds": [
        { "level": "warning", "condition": "lt", "value": 20 },
        { "level": "error", "condition": "lt", "value": 10 }
      ],
      "derivedRule": {
        "type": "linear",
        "timeUnit": "hour",
        "inputs": [{ "property": "power_draw", "coefficient": -1.0 }]
      },
      "required": true
    },
    {
      "key": "power_draw",
      "dataType": "Number",
      "unit": "kW",
      "simulationBehavior": "Settable",
      "mutability": "Mutable",
      "baseValue": 0.5,
      "constraints": { "min": 0.0, "max": 5.0 },
      "required": true
    },
    {
      "key": "altitude",
      "dataType": "Number",
      "unit": "m",
      "simulationBehavior": "Rate",
      "mutability": "Mutable",
      "baseValue": 0,
      "constraints": { "min": 0, "max": 400 },
      "required": false
    },
    {
      "key": "flight_status",
      "dataType": "String",
      "simulationBehavior": "Settable",
      "mutability": "Mutable",
      "baseValue": "grounded",
      "constraints": {
        "enum": ["grounded", "takeoff", "flying", "landing", "charging"]
      },
      "required": true
    }
  ],
  "allowedLinks": [
    { "linkType": "Monitors", "direction": "Outgoing" },
    { "linkType": "OperatedBy", "direction": "Outgoing" }
  ]
}
```

---

### Task 1-C: MongoDB seed 스크립트 생성

**파일 생성:** `infrastructure/mongo/seeds/seed_objecttypes.py`

```python
#!/usr/bin/env python3
"""
ObjectType 스키마를 MongoDB에 시딩한다.
사용법: python seed_objecttypes.py [--mongo-uri mongodb://localhost:27017] [--db mes]
"""
import json
import argparse
from pathlib import Path
from pymongo import MongoClient

SEEDS_DIR = Path(__file__).parent

def seed(mongo_uri: str, db_name: str) -> None:
    client = MongoClient(mongo_uri)
    col = client[db_name]["objectTypeSchemas"]

    for seed_file in sorted(SEEDS_DIR.glob("*_objecttype.json")):
        schema = json.loads(seed_file.read_text())
        object_type = schema["objectType"]
        result = col.replace_one({"objectType": object_type}, schema, upsert=True)
        verb = "Inserted" if result.upserted_id else "Updated"
        print(f"  [{verb}] {object_type}")

    client.close()
    print("Seeding complete.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--mongo-uri", default="mongodb://localhost:27017")
    parser.add_argument("--db",        default="mes")
    args = parser.parse_args()
    seed(args.mongo_uri, args.db)
```

**실행 방법:**

```bash
cd infrastructure/mongo/seeds
pip install pymongo
python seed_objecttypes.py --mongo-uri mongodb://localhost:27017 --db mes
```

---

### Task 1-D: Pipeline에 ObjectTypeRepository 추가

**파일 생성:** `servers/pipeline/src/repositories/mongo/object_type_repository.py`

```python
from typing import Optional
from motor.motor_asyncio import AsyncIOMotorDatabase


class ObjectTypeRepository:
    """MongoDB objectTypeSchemas 컬렉션 접근."""

    def __init__(self, db: AsyncIOMotorDatabase) -> None:
        self._col = db["objectTypeSchemas"]

    async def get_by_type(self, object_type: str) -> Optional[dict]:
        """objectType 이름으로 스키마를 조회한다. 없으면 None 반환."""
        return await self._col.find_one({"objectType": object_type}, {"_id": 0})
```

**연동:** `servers/pipeline/src/main.py` 의 lifespan 또는 dependency injection 지점에서
`AssetWorker` 생성 시 `ObjectTypeRepository(db)` 인스턴스를 주입한다.

---

### Task 1-E: asset_pipeline.py 리팩터링 (온톨로지 기반 임계값)

**파일:** `servers/pipeline/src/pipelines/asset_pipeline.py`

#### 1. 기존 `_calculate_status_from_properties()` 함수 아래에 새 함수 추가

```python
def _evaluate_alert_thresholds(properties: dict, schema: dict) -> str:
    """
    ObjectType 스키마의 ownProperties[*].alertThresholds 를 순회하여
    가장 심각한 상태('error' > 'warning' > 'normal')를 반환한다.
    """
    severity_rank = {"normal": 0, "warning": 1, "error": 2}
    worst = "normal"

    for prop_def in schema.get("ownProperties", []):
        key        = prop_def.get("key")
        thresholds = prop_def.get("alertThresholds", [])
        value      = properties.get(key)

        if value is None or not isinstance(value, (int, float)) or not thresholds:
            continue

        for t in thresholds:
            level, condition, limit = t["level"], t["condition"], t["value"]
            breached = (
                (condition == "lt"  and value <  limit) or
                (condition == "lte" and value <= limit) or
                (condition == "gt"  and value >  limit) or
                (condition == "gte" and value >= limit)
            )
            if breached and severity_rank.get(level, 0) > severity_rank.get(worst, 0):
                worst = level

    return worst
```

#### 2. `calculate_state()` 시그니처 변경 및 내부 분기 추가

```python
# 변경 전
def calculate_state(event: AssetHealthUpdatedEventDto, asset_type: str = "unknown") -> AssetStateDto:

# 변경 후
def calculate_state(
    event: AssetHealthUpdatedEventDto,
    asset_type: str = "unknown",
    schema: dict | None = None,          # ← 추가
) -> AssetStateDto:
```

`calculate_state()` 내부의 status 결정 로직:

```python
# 기존 코드 (explicit status → fallback to _calculate_status_from_properties)
status = payload.get("status") or _calculate_status_from_properties(properties)

# 변경 후
if payload.get("status"):
    status = payload["status"]
elif schema:
    status = _evaluate_alert_thresholds(properties, schema)
else:
    status = _calculate_status_from_properties(properties)  # 기존 fallback 유지
```

#### 3. Derived 속성 계산 함수 추가 (파일 하단에 추가)

```python
def calculate_derived_properties(
    current_state: dict,
    schema: dict,
    delta_seconds: float,
) -> dict:
    """
    simulationBehavior=Derived 이고 derivedRule.type=linear 인 속성들을
    delta_seconds 기준으로 계산하여 {key: new_value} dict 를 반환한다.
    current_state 를 직접 수정하지 않는다.
    """
    TIME_UNIT_TO_SECONDS = {"second": 1.0, "minute": 60.0, "hour": 3600.0}
    updates: dict = {}

    for prop_def in schema.get("ownProperties", []):
        if prop_def.get("simulationBehavior") != "Derived":
            continue

        rule = prop_def.get("derivedRule")
        if not rule or rule.get("type") != "linear":
            continue

        key         = prop_def["key"]
        current_val = current_state.get(key, prop_def.get("baseValue", 0.0))
        time_unit   = rule.get("timeUnit", "second")
        delta_units = delta_seconds / TIME_UNIT_TO_SECONDS.get(time_unit, 1.0)

        delta = sum(
            inp.get("coefficient", 1.0) * current_state.get(inp["property"], 0.0)
            for inp in rule.get("inputs", [])
        ) * delta_units

        new_val = current_val + delta

        constraints = prop_def.get("constraints", {})
        if "min" in constraints:
            new_val = max(new_val, constraints["min"])
        if "max" in constraints:
            new_val = min(new_val, constraints["max"])

        updates[key] = round(new_val, 4)

    return updates
```

---

### Task 1-F: asset_worker.py — 스키마 조회 연동

**파일:** `servers/pipeline/src/workers/asset_worker.py`

#### 1. `__init__` 에 `object_type_repo` 주입

```python
# 변경 전
def __init__(self, repo: AssetRepository, kafka_producer, ...):

# 변경 후
def __init__(self, repo: AssetRepository, object_type_repo: ObjectTypeRepository, kafka_producer, ...):
    self._object_type_repo = object_type_repo
```

#### 2. `process_health_updated()` 수정

```python
async def process_health_updated(self, event: dict) -> None:
    dto = AssetHealthUpdatedEventDto(**event)

    # 기존: asset_type 을 event 에서 직접 취득
    # 변경: MongoDB assets 컬렉션에서 asset.type 을 조회한 뒤 ObjectType 스키마 fetch
    asset_doc = await self._repo.get_asset(dto.asset_id)          # 기존 메서드 또는 추가
    asset_type = asset_doc.get("type", "unknown") if asset_doc else "unknown"
    schema = await self._object_type_repo.get_by_type(asset_type)  # None if not found

    state = calculate_state(dto, asset_type=asset_type, schema=schema)
    await self._repo.save_state(asset_state_to_dto(state))
    ...
```

**주의:** `AssetRepository` 에 `get_asset(asset_id: str) -> Optional[dict]` 메서드가 없으면 추가해야 한다.
파일: `servers/pipeline/src/repositories/mongo/asset_repository.py`

```python
async def get_asset(self, asset_id: str) -> Optional[dict]:
    return await self._db["assets"].find_one({"_id": asset_id}, {"_id": 0})
```

#### 3. `process_simulation_state_updated()` 에 Derived 계산 추가

```python
async def process_simulation_state_updated(self, event: dict) -> None:
    dto = SimulationStateUpdatedEventDto(**event)
    properties = dto.payload.get("properties", {})

    # 기존 state 저장
    state = calculate_state_from_simulation(dto)   # 기존 로직 유지

    # Derived 계산 (delta_seconds 는 payload 또는 tick interval 로부터 취득)
    asset_doc   = await self._repo.get_asset(dto.asset_id)
    asset_type  = asset_doc.get("type", "unknown") if asset_doc else "unknown"
    schema      = await self._object_type_repo.get_by_type(asset_type)

    if schema:
        delta_seconds = dto.payload.get("deltaSeconds", 1.0)
        current = {**properties, **state.__dict__}           # 현재 상태 병합
        derived = calculate_derived_properties(current, schema, delta_seconds)
        # derived 값을 state 에 반영 후 저장
        merged_properties = {**properties, **derived}
        ...
```

---

## Phase 2 — 관계 매핑 스키마 추가

### Task 2-A: openapi.json — PropertyMapping 스키마 추가

**파일:** `shared/api-schemas/openapi.json`

`components.schemas` 오브젝트 안에 아래 스키마를 추가한다.

```json
"PropertyMapping": {
  "type": "object",
  "required": ["fromProperty", "toProperty"],
  "properties": {
    "fromProperty":   { "type": "string", "description": "source asset 속성 key" },
    "toProperty":     { "type": "string", "description": "target asset 속성 key" },
    "transformRule":  {
      "type": "string",
      "description": "변환 표현식. 'value', 'value * N', 'value / N' 형식만 지원",
      "default": "value"
    },
    "fromUnit": { "type": "string" },
    "toUnit":   { "type": "string" }
  }
}
```

`CreateRelationshipRequest` 와 `RelationshipDto` 스키마에 아래 필드를 추가한다.

```json
"mappings": {
  "type": "array",
  "items": { "$ref": "#/components/schemas/PropertyMapping" },
  "default": [],
  "description": "속성 매핑 목록. 비어있으면 기존 TransferSpec(properties 필드) 방식으로 동작한다."
}
```

---

### Task 2-B: 백엔드 RelationshipDto.cs — Mappings 필드 추가

**파일:** `servers/backend/DotnetEngine/Application/Relationship/Dto/RelationshipDto.cs`

기존 record 선언 위에 `PropertyMapping` record를 추가하고, `RelationshipDto`에 `Mappings` 추가.

```csharp
public record PropertyMapping(
    string FromProperty,
    string ToProperty,
    string TransformRule = "value",
    string? FromUnit = null,
    string? ToUnit = null
);

// 기존 RelationshipDto record에 Mappings 파라미터 추가
public record RelationshipDto(
    string Id,
    string FromAssetId,
    string ToAssetId,
    string RelationshipType,
    Dictionary<string, object?> Properties,
    IReadOnlyList<PropertyMapping> Mappings,   // ← 추가
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

RelationshipDto 를 생성하는 모든 mapper/factory 코드에서 `Mappings` 파라미터를 추가해야 한다.
MongoDB document 에서 `mappings` 배열 필드를 역직렬화하여 전달하면 된다.
필드가 없는 기존 document 의 경우 `[]` 로 기본값을 제공한다.

---

### Task 2-C: SuppliesRule.cs — Mappings 기반 전파 로직 추가

**파일:** `servers/backend/DotnetEngine/Application/Simulation/Rules/SuppliesRule.cs`

기존 `Apply()` 메서드 내부 수정: `relationship.Mappings` 가 비어있지 않으면 Mappings 방식 사용, 아니면 기존 `TransferSpecParser` fallback.

```csharp
public PropagationResult Apply(PropagationContext ctx)
{
    var rel = ctx.Relationship;
    Dictionary<string, object?> transferred;

    if (rel.Mappings is { Count: > 0 })
    {
        transferred = ApplyMappings(rel.Mappings, ctx.IncomingState.Properties);
    }
    else
    {
        // 기존 TransferSpecParser 방식 (하위 호환)
        var specs = TransferSpecParser.Parse(rel.Properties);
        transferred = BuildTransferred(specs, ctx.IncomingState.Properties);
    }

    // 이하 기존 patch 생성 및 이벤트 발행 로직 유지
    ...
}

private static Dictionary<string, object?> ApplyMappings(
    IReadOnlyList<PropertyMapping> mappings,
    Dictionary<string, object?> source)
{
    var result = new Dictionary<string, object?>();
    foreach (var m in mappings)
    {
        if (!source.TryGetValue(m.FromProperty, out var raw)) continue;
        var value = Convert.ToDouble(raw);
        result[m.ToProperty] = ApplyTransform(m.TransformRule, value);
    }
    return result;
}

private static double ApplyTransform(string rule, double value)
{
    // 지원 형식: "value", "value * N", "value / N", "value + N", "value - N"
    if (string.IsNullOrWhiteSpace(rule) || rule.Trim() == "value") return value;

    var parts = rule.Trim().Split(' ');
    if (parts.Length == 3 && parts[0] == "value" && double.TryParse(parts[2], out var operand))
    {
        return parts[1] switch
        {
            "*" => value * operand,
            "/" => operand != 0 ? value / operand : value,
            "+" => value + operand,
            "-" => value - operand,
            _   => value
        };
    }
    return value;
}
```

---

### Task 2-D: infrastructure/mongo/MODEL.md 업데이트

**파일:** `infrastructure/mongo/MODEL.md`

`relationships` 컬렉션 문서 내 필드 목록에 아래를 추가한다.

```markdown
- mappings: Array (optional)
  - fromProperty: string — source asset 속성 key
  - toProperty: string — target asset 속성 key
  - transformRule: string — 변환 표현식 (기본값: "value")
  - fromUnit: string (optional)
  - toUnit: string (optional)
  * 비어있으면 기존 properties.transfers 방식으로 동작한다 (하위 호환)
```

---

## Phase 3 — 프론트엔드 최소 개선

### Task 3-A: UnitSelect 공용 컴포넌트 생성

**파일 생성:** `servers/frontend/src/shared/ui/UnitSelect.tsx`

```tsx
import { useState, useMemo } from 'react'

const BASE_UNITS = ['m', 'kg', 's', 'A', 'K', 'W', 'Wh', 'L', 'item']
const DERIVED_UNITS = ['%', 'mps', 'kW', 'kWh', 'itemsPerHour', 'degC', 'degF']

interface UnitSelectProps {
  value?: string
  onChange: (unit: string) => void
  placeholder?: string
  style?: React.CSSProperties
}

export function UnitSelect({
  value,
  onChange,
  placeholder = '단위 선택',
  style,
}: UnitSelectProps) {
  const [search, setSearch] = useState('')

  const filteredBase = useMemo(
    () =>
      BASE_UNITS.filter((u) => u.toLowerCase().includes(search.toLowerCase())),
    [search],
  )
  const filteredDerived = useMemo(
    () =>
      DERIVED_UNITS.filter((u) =>
        u.toLowerCase().includes(search.toLowerCase()),
      ),
    [search],
  )

  const inputStyle: React.CSSProperties = {
    padding: '4px 8px',
    borderRadius: 4,
    border: '1px solid #555',
    background: '#1a1a1a',
    color: '#fff',
    width: '100%',
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4, ...style }}>
      <input
        type="text"
        placeholder="단위 검색..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        style={inputStyle}
      />
      <select
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value)}
        style={inputStyle}
      >
        <option value="">{placeholder}</option>
        {filteredBase.length > 0 && (
          <optgroup label="기본 단위">
            {filteredBase.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </optgroup>
        )}
        {filteredDerived.length > 0 && (
          <optgroup label="파생 단위">
            {filteredDerived.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </optgroup>
        )}
      </select>
    </div>
  )
}
```

---

### Task 3-B: ObjectType 속성 편집 폼에 UnitSelect 연동

**파일:** ObjectType 속성을 생성/편집하는 폼 컴포넌트
(위치 확인: `src/pages/settings/ui/` 또는 `src/features/objectType/` 내 속성 편집 컴포넌트)

각 PropertyDefinition row에서 `unit` 필드를 자유 텍스트 input 대신 `<UnitSelect>` 로 교체한다.

```tsx
import { UnitSelect } from '@/shared/ui/UnitSelect'

// 기존 unit input
<input value={prop.unit ?? ''} onChange={...} placeholder="단위" />

// 변경 후
<UnitSelect
  value={prop.unit}
  onChange={unit => updateProp(idx, { ...prop, unit })}
/>
```

---

### Task 3-C: RelationshipsPage.tsx — 속성 매핑 UI 추가

**파일:** `servers/frontend/src/pages/relationships/ui/RelationshipsPage.tsx`

#### 1. 필요한 타입 및 state 추가

```tsx
interface PropertyMapping {
  fromProperty: string
  toProperty: string
  transformRule: string
  fromUnit?: string
  toUnit?: string
}

// 폼 state에 추가
const [mappings, setMappings] = useState<PropertyMapping[]>([])
const [fromSchema, setFromSchema] = useState<any>(null)
const [toSchema, setToSchema] = useState<any>(null)
```

#### 2. fromAsset / toAsset 선택 시 ObjectType 스키마 fetch

```tsx
// fromAssetId 가 선택될 때마다 실행
useEffect(() => {
  if (!formData.fromAssetId) return
  const asset = assets.find((a) => a.id === formData.fromAssetId)
  if (!asset?.type) return
  fetch(`/api/object-type-schemas/${asset.type}`)
    .then((r) => r.json())
    .then(setFromSchema)
    .catch(() => setFromSchema(null))
}, [formData.fromAssetId, assets])

// toAssetId 도 동일하게
useEffect(() => {
  if (!formData.toAssetId) return
  const asset = assets.find((a) => a.id === formData.toAssetId)
  if (!asset?.type) return
  fetch(`/api/object-type-schemas/${asset.type}`)
    .then((r) => r.json())
    .then(setToSchema)
    .catch(() => setToSchema(null))
}, [formData.toAssetId, assets])
```

#### 3. 매핑 UI 렌더링 — 폼 안에 삽입 (Properties 텍스트영역 아래)

```tsx
{
  fromSchema && toSchema && (
    <div style={{ marginTop: 12 }}>
      <div style={{ fontWeight: 600, marginBottom: 6 }}>속성 매핑</div>

      {mappings.map((m, idx) => {
        const fromProps: any[] = fromSchema.ownProperties ?? []
        const toProps: any[] = toSchema.ownProperties ?? []
        const fromProp = fromProps.find((p) => p.key === m.fromProperty)
        const toProp = toProps.find((p) => p.key === m.toProperty)
        const unitMismatch =
          fromProp?.unit && toProp?.unit && fromProp.unit !== toProp.unit

        return (
          <div
            key={idx}
            style={{
              display: 'flex',
              gap: 6,
              alignItems: 'center',
              marginBottom: 6,
            }}
          >
            {/* Source property */}
            <select
              value={m.fromProperty}
              onChange={(e) => {
                const updated = [...mappings]
                const fp = fromProps.find((p) => p.key === e.target.value)
                updated[idx] = {
                  ...m,
                  fromProperty: e.target.value,
                  fromUnit: fp?.unit,
                }
                setMappings(updated)
              }}
            >
              <option value="">source 속성</option>
              {fromProps.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.key} {p.unit ? `(${p.unit})` : ''}
                </option>
              ))}
            </select>

            <span>→</span>

            {/* Target property */}
            <select
              value={m.toProperty}
              onChange={(e) => {
                const updated = [...mappings]
                const tp = toProps.find((p) => p.key === e.target.value)
                updated[idx] = {
                  ...m,
                  toProperty: e.target.value,
                  toUnit: tp?.unit,
                }
                setMappings(updated)
              }}
            >
              <option value="">target 속성</option>
              {toProps.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.key} {p.unit ? `(${p.unit})` : ''}
                </option>
              ))}
            </select>

            {/* Transform rule */}
            <input
              value={m.transformRule}
              onChange={(e) => {
                const updated = [...mappings]
                updated[idx] = { ...m, transformRule: e.target.value }
                setMappings(updated)
              }}
              placeholder="value * 1.0"
              style={{ width: 110 }}
            />

            {/* Unit mismatch warning */}
            {unitMismatch && (
              <span
                title={`단위 불일치: ${fromProp.unit} ≠ ${toProp.unit}`}
                style={{ color: '#facc15' }}
              >
                ⚠️
              </span>
            )}

            <button
              onClick={() => setMappings(mappings.filter((_, i) => i !== idx))}
            >
              ✕
            </button>
          </div>
        )
      })}

      <button
        type="button"
        onClick={() =>
          setMappings([
            ...mappings,
            { fromProperty: '', toProperty: '', transformRule: 'value' },
          ])
        }
      >
        + 매핑 추가
      </button>
    </div>
  )
}
```

#### 4. createRelationship / updateRelationship 호출 시 mappings 포함

```tsx
// createRelationship 호출부
await createRelationship({
  fromAssetId: formData.fromAssetId,
  toAssetId: formData.toAssetId,
  relationshipType: formData.relationshipType,
  properties: JSON.parse(formData.properties || '{}'),
  mappings, // ← 추가
})
```

---

### Task 3-D: 시뮬레이션 실시간 값 표시 수정

**목표:** SimulationPage에서 SSE 이벤트가 올 때 각 Asset의 속성 값이 실시간으로 화면에 반영된다.

**현상:** `subscribeSimulationEvents()` 가 SSE로 `SimulationTickEvent` 를 받고 있지만,
이벤트 로그 테이블만 업데이트되고 Asset 카드의 속성 값은 업데이트되지 않음.

**파일:** `servers/frontend/src/pages/simulation/ui/SimulationPage.tsx`

#### 1. liveStates state 추가

```tsx
// assetId → { propertyKey: value } 실시간 맵
const [liveStates, setLiveStates] = useState<
  Record<string, Record<string, unknown>>
>({})
```

#### 2. SSE 콜백에서 liveStates 업데이트

```tsx
// subscribeSimulationEvents() 콜백 내부
const unsubscribe = subscribeSimulationEvents((event) => {
  // 기존 이벤트 로그 추가 로직 유지 ...

  // 실시간 상태 업데이트 추가
  if (event.assetId && event.payload?.properties) {
    setLiveStates((prev) => ({
      ...prev,
      [event.assetId]: {
        ...(prev[event.assetId] ?? {}),
        ...event.payload.properties,
      },
    }))
  }
})
```

#### 3. 실시간 상태 패널 렌더링 — 이벤트 로그 위에 삽입

```tsx
{
  Object.keys(liveStates).length > 0 && (
    <div style={{ marginBottom: 16 }}>
      <h3>실시간 Asset 상태</h3>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
        {Object.entries(liveStates).map(([assetId, props]) => (
          <div
            key={assetId}
            style={{
              border: '1px solid #444',
              borderRadius: 8,
              padding: '8px 12px',
              minWidth: 180,
              background: '#1a1a1a',
            }}
          >
            <div style={{ fontWeight: 700, marginBottom: 4, fontSize: 13 }}>
              {assets.find((a) => a.id === assetId)?.id ?? assetId}
            </div>
            {Object.entries(props).map(([k, v]) => (
              <div key={k} style={{ fontSize: 12, color: '#aaa' }}>
                {k}: <span style={{ color: '#fff' }}>{String(v)}</span>
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  )
}
```

**추가 수정 — 홈 페이지 ReactFlow 노드 카드 (빈 값 문제 해결):**

**파일:** `servers/frontend/src/pages/home/ui/` 내 ReactFlow 노드 컴포넌트

노드 렌더링 시 `data.state?.properties` 또는 `data.state?.currentTemp` 등을 읽고 있는데,
초기 로딩 시 states API 응답에서 properties 가 비어있거나 매핑 키가 다른 경우.

수정 방향:

1. states API 응답 (`GET /api/states`)의 `properties` 필드와 노드 카드가 표시하는 키 이름이 일치하는지 확인
2. 일치하지 않으면 노드 렌더링 코드에서 `state.properties ?? {}` 를 순회하여 동적으로 `key: value` 표시
3. 빈 값일 때는 `—` 처리

```tsx
// 기존 하드코딩 예시
<div>temperature: {state?.currentTemp}</div>
<div>power consume: {state?.currentPower}</div>

// 변경 후 — 동적 렌더링
{Object.entries(state?.properties ?? {}).map(([k, v]) => (
  <div key={k} style={{ fontSize: 11, color: '#aaa' }}>
    {k}: <span style={{ color: '#fff' }}>{v != null ? String(v) : '—'}</span>
  </div>
))}
```

---

## 실행 순서 요약

```
1-A → 1-B → 1-C   (스키마 + 시드 파일 준비, 병렬 가능)
1-D → 1-E → 1-F   (파이프라인 코드 변경, 순서 의존)
2-A → 2-B → 2-C   (관계 매핑, 순서 의존)
2-D                (MODEL.md, 독립)
3-A → 3-B          (UnitSelect 먼저, 이후 ObjectType 폼 연동)
3-C                (RelationshipsPage, 3-A 이후)
3-D                (SimulationPage, 독립)
```

Phase 1 → Phase 2 → Phase 3 순으로 진행하되,
각 Phase 내 독립 Task는 병렬로 처리 가능.
