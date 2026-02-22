namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// Replay 실행 결과 (POST /api/simulation/runs/{runId}/replay).
/// </summary>
public sealed record ReplayRunResult
{
    public bool Success { get; init; }
    public string RunId { get; init; } = string.Empty;
    public int? TickMax { get; init; }
    public int EventsApplied { get; init; }
    public string? Message { get; init; }
}
