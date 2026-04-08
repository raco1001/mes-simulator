using DotnetEngine.Domain.Simulation;

namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 실행 요청 (POST /api/simulation/runs body).
/// RunTick: 전파 1회당 Run 전역 tick. 단건 실행은 0, 엔진은 해당 tick 전달.
/// Provide <see cref="TriggerAssetIds"/> (preferred) and/or legacy <see cref="TriggerAssetId"/>.
/// </summary>
public sealed record RunSimulationRequest
{
    /// <summary>Legacy single seed.</summary>
    public string? TriggerAssetId { get; init; }

    /// <summary>One or more seeds; non-empty list takes precedence over <see cref="TriggerAssetId"/>.</summary>
    public IReadOnlyList<string>? TriggerAssetIds { get; init; }

    public StatePatchDto? Patch { get; init; }
    /// <summary>≤0이면 <see cref="SimulationEngineConstants.DefaultLeafPropagationMaxDepth"/>.</summary>
    public int MaxDepth { get; init; }
    /// <summary>Run 전역 tick (이벤트 payload.tick에 포함). 단건 실행 시 0.</summary>
    public int RunTick { get; init; }
    /// <summary>연속 시뮬 엔진 폴링 주기(ms). 1–5000.</summary>
    public int EngineTickIntervalMs { get; init; } = SimulationEngineConstants.DefaultEngineTickIntervalMs;

    /// <summary>
    /// true이면 참여 에셋의 Mongo <c>states</c>를 삭제하고(What-if dryRun 제외) 에셋·스키마 기준으로 시드한다.
    /// </summary>
    public bool ResetState { get; init; }
}
