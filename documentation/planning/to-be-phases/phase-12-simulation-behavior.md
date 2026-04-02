# Phase 12 — SimulationBehavior 기반 엔진 확장 (Layer 3a)

**as-is Phase 10.3과 거의 동일. IPropertySimulator 전략 패턴 + dry-run 가능 구조 준비.**

---

## 1. 목표

속성의 `SimulationBehavior`에 따라 tick 계산 방법을 분리한다. 기존 `IPropagationRule`(관계 기반 전파)과 새 `IPropertySimulator`(속성별 tick 계산)의 **관심사 분리**가 핵심이다.

추가로, Phase 15에서 사용할 dry-run(What-if) 모드의 분기 포인트를 이 Phase에서 미리 마련한다.

### as-is 대비 변경

| 항목 | as-is (Phase 10.3) | to-be 추가분 |
| --- | --- | --- |
| IPropertySimulator 5개 | 동일 | 동일 |
| Handler 리팩토링 | 동일 | 동일 |
| dry-run 구조 | 없음 | `EngineStateApplier`에 분기 포인트 준비 |

---

## 2. 선행 조건

- Phase 11 완료 (동적 Properties + ObjectTypeSchema)

---

## 3. 아키텍처

```
SimulationEngineService (tick 루프)
  │
  ├─ RunSimulationCommandHandler (BFS 전파)
  │    │
  │    ├─ 각 노드에서: IPropertySimulator로 속성별 tick 계산
  │    │    ├─ ConstantSimulator: 아무것도 안 함
  │    │    ├─ SettableSimulator: 패치가 있을 때만 적용
  │    │    ├─ RateSimulator: value += delta * dt
  │    │    ├─ AccumulatorSimulator: stored += inflow - outflow (min/max)
  │    │    └─ DerivedSimulator: 의존 속성으로부터 재계산
  │    │
  │    └─ 관계 전파 시: IPropagationRule로 전달할 속성 결정
  │
  └─ EngineStateApplier (UpsertState → AppendEvent → Publish)
       └─ [NEW] dryRun 플래그: true이면 UpsertState/Publish 생략, 결과만 반환
```

---

## 4. IPropertySimulator 인터페이스

```csharp
public interface IPropertySimulator
{
    SimulationBehavior Behavior { get; }
    object? Compute(PropertySimulationContext ctx);
}

public record PropertySimulationContext
{
    public required PropertyDefinition Definition { get; init; }
    public object? CurrentValue { get; init; }
    public object? PatchValue { get; init; }
    public TimeSpan DeltaTime { get; init; }
    public IReadOnlyDictionary<string, object> AllProperties { get; init; } = new Dictionary<string, object>();
}
```

---

## 5. 구현체

### 5.1 ConstantSimulator

`CurrentValue`를 그대로 반환. `PatchValue`가 있어도 무시. Phase 11의 `immutable` 검증과 이중 보호.

```csharp
public object? Compute(PropertySimulationContext ctx) => ctx.CurrentValue;
```

### 5.2 SettableSimulator

`PatchValue ?? CurrentValue` 반환. 외부 패치가 들어올 때만 변경.

```csharp
public object? Compute(PropertySimulationContext ctx)
    => ctx.PatchValue ?? ctx.CurrentValue;
```

### 5.3 RateSimulator

현재 값에 변화율을 시간 기반으로 적용.

```csharp
public object? Compute(PropertySimulationContext ctx)
{
    var current = Convert.ToDouble(ctx.CurrentValue ?? ctx.Definition.BaseValue ?? 0);
    var delta = Convert.ToDouble(ctx.PatchValue ?? ctx.Definition.BaseValue ?? 0);
    return current + delta * ctx.DeltaTime.TotalSeconds;
}
```

`PatchValue`는 delta(변화율) 값. 기본값은 `Definition.BaseValue`.

### 5.4 AccumulatorSimulator

초기 저장량에서 inflow/outflow에 의해 증감. min/max 클램핑.

```csharp
public object? Compute(PropertySimulationContext ctx)
{
    var stored = Convert.ToDouble(ctx.CurrentValue ?? ctx.Definition.BaseValue ?? 0);
    var flow = Convert.ToDouble(ctx.PatchValue ?? 0);
    var result = stored + flow;

    if (ctx.Definition.Constraints is { } c)
    {
        if (c.Min.HasValue) result = Math.Max(c.Min.Value, result);
        if (c.Max.HasValue) result = Math.Min(c.Max.Value, result);
    }
    return result;
}
```

`PatchValue`는 net flow (inflow - outflow). 관계로부터 전달받은 값이 여기에 합산됨.

### 5.5 DerivedSimulator

다른 속성으로부터 계산. Phase 12 구현에서는 `constraints.dependsOn` + `constraints.operation(sum|avg|min|max)`를 지원한다.

지원 연산이 없거나 참조값이 없으면 `CurrentValue ?? BaseValue`로 fallback.

---

## 6. Handler 리팩토링

`RunSimulationCommandHandler.RunOnePropagationAsync`의 노드 처리 블록:

1. 대상 에셋의 `ObjectTypeSchema` 조회
2. 스키마 미존재 시 → 기존 동작 유지 (패치 덮어쓰기)
3. 스키마 존재 시 → 각 `PropertyDefinition`에 대해:
   - 해당 `SimulationBehavior`의 `IPropertySimulator` 조회
   - `PropertySimulationContext` 구성 (현재값, 패치값, 시간, 전체 속성)
   - `Compute()` 호출
   - 결과를 새 `Properties` 딕셔너리에 반영
4. 기존 `MergeState` → `ComputeState`로 교체

### DI 등록

```csharp
services.AddSingleton<IPropertySimulator, ConstantSimulator>();
services.AddSingleton<IPropertySimulator, SettableSimulator>();
services.AddSingleton<IPropertySimulator, RateSimulator>();
services.AddSingleton<IPropertySimulator, AccumulatorSimulator>();
services.AddSingleton<IPropertySimulator, DerivedSimulator>();
```

`SimulationBehavior` → `IPropertySimulator` 매핑은 `IEnumerable<IPropertySimulator>`에서 `Behavior` 프로퍼티로 자동 해결.

---

## 7. dry-run 분기 포인트 준비

Phase 15에서 What-if 시뮬레이션이 필요하다. 이 Phase에서 `EngineStateApplier`에 `dryRun` 플래그를 받을 수 있는 구조를 마련한다.

### 변경 범위 (최소한)

```csharp
// EngineStateApplier
public async Task<ApplyResult> ApplyAsync(
    ...,
    bool dryRun = false,
    CancellationToken cancellationToken = default)
{
    if (!dryRun)
    {
        await _stateRepository.UpsertAsync(newState, cancellationToken);
        await _eventRepository.AppendAsync(evt, cancellationToken);
        await _publisher.PublishAsync(evt, cancellationToken);
    }

    return new ApplyResult { State = newState, Event = evt };
}
```

- `dryRun = true`일 때: 상태 계산은 수행하되, 영구 저장과 이벤트 발행을 건너뜀
- Phase 12에서는 `dryRun` 파라미터 추가만 하고, 실제 호출은 Phase 15에서 진행

---

## 8. 테스트

| 테스트 | 검증 내용 |
| --- | --- |
| `ConstantSimulatorTests` | 패치가 있어도 값 불변 |
| `SettableSimulatorTests` | 패치 시 값 변경, 패치 없으면 유지 |
| `RateSimulatorTests` | `value += delta * dt` 검증, 다양한 dt |
| `AccumulatorSimulatorTests` | 증감 + min/max 클램핑 |
| `DerivedSimulatorTests` | 기본값 반환 (MVP) |
| `RunSimulationCommandHandlerTests` | behavior별 분기 동작, 스키마 없는 에셋 하위 호환 |
| `EngineStateApplierTests` | `dryRun=true` 시 저장/발행 생략 검증 |

---

## 9. 거버넌스

- 새 `SimulationBehavior` enum 값 추가 시 대응 `IPropertySimulator` 구현이 필수 (거버넌스 규칙 7)
- DI에서 미등록 behavior에 대한 fallback 전략: 경고 로그 + `SettableSimulator`로 대체

---

## 10. 완료 기준

- [x] `IPropertySimulator` 인터페이스 + 5개 구현체 존재
- [x] `RunSimulationCommandHandler`가 ObjectTypeSchema 기반으로 behavior별 계산 수행
- [x] `rate` 속성이 `value += delta * dt`로 변화
- [x] `accumulator` 속성이 min/max 내에서 증감
- [x] `constant` 속성은 절대 변하지 않음
- [x] 스키마 없는 에셋은 기존 동작 유지 (하위 호환)
- [x] `EngineStateApplier`에 `dryRun` 파라미터 추가 (호출 없이 구조만)
- [x] 모든 테스트 통과

---

## 11. 산출물

| 산출물 | 변경 유형 |
| --- | --- |
| `IPropertySimulator.cs` | 신규 |
| `PropertySimulationContext.cs` | 신규 |
| `ConstantSimulator.cs` | 신규 |
| `SettableSimulator.cs` | 신규 |
| `RateSimulator.cs` | 신규 |
| `AccumulatorSimulator.cs` | 신규 |
| `DerivedSimulator.cs` | 신규 |
| `RunSimulationCommandHandler.cs` | 수정 (ComputeState 도입) |
| `EngineStateApplier.cs` | 수정 (dryRun 파라미터) |
| DI 등록 (Program.cs 등) | 수정 |
| 테스트 7건 | 신규/수정 |

---

## 12. 확장 시 변경 예상

| 규모 변화 | 현재 설계 | 확장 시 변경 |
| --- | --- | --- |
| 복잡한 derived 속성 | 단순 기본값 반환 | 수식 엔진 (expression evaluator) 도입 |
| 대규모 에셋 네트워크 | 모든 속성 순차 계산 | 속성 의존 그래프 + 병렬 계산 |
| 새 behavior 추가 | enum 확장 + 구현체 추가 | 플러그인 아키텍처 (별도 어셈블리) |

---

## 13. 참고

- [Phase 11 — 동적 State + ObjectTypeSchema](phase-11-dynamic-state-objecttype.md)
- [as-is Phase 10.3 — SimulationBehavior 엔진](../as-is-phases/2026-03-27-development-plan.md#7-phase-103--simulationbehavior-기반-엔진-확장)
