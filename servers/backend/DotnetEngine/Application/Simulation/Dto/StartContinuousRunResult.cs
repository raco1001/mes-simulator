namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 지속 시뮬레이션 시작 결과 (POST /api/simulation/runs/start).
/// </summary>
public sealed record StartContinuousRunResult
{
    public required bool Success { get; init; }
    public required string RunId { get; init; }
    public string? Message { get; init; }
}
