using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation;

public sealed record PropertySimulationContext
{
    public required PropertyDefinition Definition { get; init; }
    public object? CurrentValue { get; init; }
    public object? PatchValue { get; init; }
    public TimeSpan DeltaTime { get; init; }
    public IReadOnlyDictionary<string, object?> AllProperties { get; init; } = new Dictionary<string, object?>();
}
