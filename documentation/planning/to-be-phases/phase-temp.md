현재 준비 상태 평가
작동하는 것들 ✅
시뮬레이션 인프라 자체는 잘 만들어져 있습니다. 5가지 시뮬레이터(Constant, Settable, Rate, Accumulator, Derived)가 모두 구현되어 있고, 3가지 전파 규칙(ConnectedTo, Supplies, ContainsRule)도 있습니다. 단발 시뮬레이션, WhatIf, 연속 시뮬레이션, SSE 스트림, 리플레이까지 API가 갖춰져 있어 기본 동작 테스트는 지금도 가능합니다.

언급한 두 요구사항 — 모두 현재 미지원
① 1초 미만 tick 설정 불가
두 곳이 막혀 있습니다.
첫 번째는 SimulationEngineService의 백그라운드 워커 폴링 주기입니다.
csharp// Workers/SimulationEngineService.cs
private const int TickIntervalMs = 1000; // 하드코딩된 상수
이 값이 1000ms로 고정되어 있으므로, 아무리 에셋 메타데이터에 tickIntervalMs: 200을 넣어도 실제 워커가 깨어나는 주기는 1초입니다.
두 번째는 ComputeState의 DeltaTime입니다.
csharp// Handlers/RunSimulationCommandHandler.cs
var computed = simulator.Compute(new PropertySimulationContext
{
DeltaTime = TimeSpan.FromSeconds(1), // 항상 1초 고정
...
});
RateSimulator는 current + (delta \* DeltaTime.TotalSeconds)로 계산합니다. 실제로 200ms마다 tick이 돌아도 이 공식에서 delta는 항상 × 1.0이 됩니다. 즉 sub-second tick을 지원하더라도 Rate/Accumulator 계산 결과가 틀립니다.
② 임의 에셋의 독립 전파 불가
연속 시뮬레이션에서 모든 참여 에셋이 동시에 due가 아닐 때의 로직을 보면:
csharp// Workers/SimulationEngineService.cs
if (due.Count == participating.Count)
{
// 전체 due → TriggerAssetId 기준 BFS 전파 (MaxDepth 적용)
await command.RunOnePropagationAsync(run.Id, request, ...);
}
else
{
foreach (var assetId in due)
{
var request = new RunSimulationRequest
{
TriggerAssetId = assetId,
MaxDepth = 0, // ← due인 에셋 각자 상태만 갱신, 하위 전파 없음
};
}
}
에셋별 tick 주기가 다를 때, 일부만 due가 되면 MaxDepth: 0으로 처리합니다. 해당 에셋의 상태는 갱신되지만 관계 하위 에셋으로 전파가 일어나지 않습니다. 연속 시뮬레이션에서 임의의 에셋이 트리거가 되어 독립적으로 전파하려면 이 로직이 바뀌어야 합니다.
단, POST /api/simulation/runs(단발)나 POST /api/simulation/runs/start(연속)에서 triggerAssetId를 어떤 에셋으로든 지정하는 것 자체는 이미 가능합니다. BFS가 그 에셋에서 시작되므로 그래프 상 어디서든 루트를 잡을 수 있습니다.

구현이 필요한 것
Fix 1: 백그라운드 워커 tick 주기 — 설정 가능하게
파일: SimulationEngineService.cs
const int TickIntervalMs = 1000을 환경변수 또는 SimulationRunDto의 설정값으로 교체합니다.
가장 단순한 방향은 SimulationRunDto에 EngineTickIntervalMs 필드를 추가하고 RunSimulationRequest에서 받아 저장하는 방식입니다. SimulationEngineService는 각 Run의 이 값을 읽어 Task.Delay에 사용합니다.
csharp// SimulationRunDto에 추가
public int EngineTickIntervalMs { get; init; } = 1000;

// RunSimulationRequest에 추가
public int EngineTickIntervalMs { get; init; } = 1000;

// SimulationEngineService에서
await Task.Delay(run.EngineTickIntervalMs, stoppingToken);
Fix 2: DeltaTime — 실제 경과시간으로 교체
파일: RunSimulationCommandHandler.cs
ComputeState에 DeltaTime 파라미터를 추가하고, 호출 시 실제 경과 시간을 계산해 전달합니다.
csharpprivate StateDto ComputeState(
string assetId,
StateDto? current,
StatePatchDto patch,
ObjectTypeSchemaDto? objectTypeSchema,
AssetDto? asset,
TimeSpan deltaTime) // 추가

// DeltaTime 계산 (RunOnePropagationAsync에서)
var lastUpdated = currentState?.UpdatedAt ?? run.StartedAt;
var deltaTime = DateTimeOffset.UtcNow - lastUpdated;
var mergedState = ComputeState(assetId, currentState, patch, objectTypeSchema, asset, deltaTime);
Fix 3: 부분 due일 때도 MaxDepth 유지
파일: SimulationEngineService.cs
due 에셋이 일부만 있을 때 각각의 MaxDepth를 0이 아닌 Run의 MaxDepth로 설정합니다.
csharpforeach (var assetId in due)
{
var request = new RunSimulationRequest
{
TriggerAssetId = assetId,
MaxDepth = run.MaxDepth, // 0 → run.MaxDepth
Patch = null,
RunTick = nextTick,
};
await command.RunOnePropagationAsync(run.Id, request, ...);
}
이 변경 하나로 각 due 에셋이 독립적으로 하위 관계까지 전파할 수 있게 됩니다.

요약
항목현재 상태필요 작업단발 시뮬레이션✅ 동작없음임의 에셋을 run 루트로 지정✅ 동작없음WhatIf / 리플레이✅ 동작없음SSE 스트림✅ 동작없음sub-second tick❌ 불가Fix 1 (워커 주기 설정화)Rate/Accumulator 정확도❌ 항상 1s deltaFix 2 (실제 DeltaTime)due 에셋의 독립 전파❌ MaxDepth=0 고정Fix 3 (MaxDepth 유지)
Fix 3은 코드 한 줄 변경입니다. Fix 1·2는 RunSimulationRequest와 SimulationRunDto에 필드를 추가하고, 워커와 핸들러에서 해당 값을 사용하도록 연결하는 작업입니다. 백엔드에만 해당되고 프론트·파이프라인 변경은 없습니다.
