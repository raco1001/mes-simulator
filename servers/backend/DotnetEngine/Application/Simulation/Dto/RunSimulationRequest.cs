namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 실행 요청 (POST /api/simulation/runs body).
/// </summary>
public sealed record RunSimulationRequest
{
    public required string TriggerAssetId { get; init; }
    public StatePatchDto? Patch { get; init; }
    public int MaxDepth { get; init; } = 3;
}
