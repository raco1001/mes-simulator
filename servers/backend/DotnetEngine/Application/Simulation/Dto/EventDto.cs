namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 도메인 이벤트 DTO (events 컬렉션 append용).
/// </summary>
public sealed record EventDto
{
    public required string AssetId { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? SimulationRunId { get; init; }
    /// <summary>Run-global tick when this event was produced (for replay ordering).</summary>
    public int? RunTick { get; init; }
    public string? RelationshipId { get; init; }
    public IReadOnlyDictionary<string, object> Payload { get; init; } = new Dictionary<string, object>();
}
