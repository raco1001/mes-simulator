namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// Run 중 속성 변조(override) 한 건. 재현 시 fromTick 이후 적용.
/// </summary>
public sealed record SimulationOverrideEntryDto
{
    public required string AssetId { get; init; }
    public required string PropertyKey { get; init; }
    public required object Value { get; init; }
    public required int FromTick { get; init; }
    public int? ToTick { get; init; }
}
