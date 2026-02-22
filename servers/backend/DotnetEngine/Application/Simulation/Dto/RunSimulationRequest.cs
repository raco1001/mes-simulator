namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 실행 요청 (POST /api/simulation/runs body).
/// RunTick: 전파 1회당 Run 전역 tick. 단건 실행은 0, 엔진은 해당 tick 전달.
/// </summary>
public sealed record RunSimulationRequest
{
    public required string TriggerAssetId { get; init; }
    public StatePatchDto? Patch { get; init; }
    public int MaxDepth { get; init; } = 3;
    /// <summary>Run 전역 tick (이벤트 payload.tick에 포함). 단건 실행 시 0.</summary>
    public int RunTick { get; init; }
}
