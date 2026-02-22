using DotnetEngine.Application.Simulation;

namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 런 세션 DTO (트리거 + 1회 전파 실행 단위).
/// </summary>
public sealed record SimulationRunDto
{
    public required string Id { get; init; }
    public required SimulationRunStatus Status { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public required string TriggerAssetId { get; init; }
    /// <summary>상태 패치 표현 (currentTemp, currentPower, status, lastEventType 등).</summary>
    public IReadOnlyDictionary<string, object> Trigger { get; init; } = new Dictionary<string, object>();
    public int MaxDepth { get; init; }
}
