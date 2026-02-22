namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 런 중단 결과 (POST /api/simulation/runs/{runId}/stop).
/// </summary>
public sealed record StopSimulationRunResult
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
}
