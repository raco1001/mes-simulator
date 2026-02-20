using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 전파 룰 적용 시 사용하는 컨텍스트 (from, relationship, to, patch).
/// </summary>
public sealed record PropagationContext
{
    public required string FromAssetId { get; init; }
    public StateDto? FromState { get; init; }
    public required RelationshipDto Relationship { get; init; }
    public required string ToAssetId { get; init; }
    public StateDto? ToState { get; init; }
    public required StatePatchDto IncomingPatch { get; init; }
    public int Depth { get; init; }
    public required string SimulationRunId { get; init; }
}
