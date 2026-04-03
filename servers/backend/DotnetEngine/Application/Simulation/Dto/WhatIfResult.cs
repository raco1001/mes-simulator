namespace DotnetEngine.Application.Simulation.Dto;

public sealed record WhatIfResult
{
    public required string RunId { get; init; }
    public required IReadOnlyDictionary<string, StateSnapshot> Before { get; init; }
    public required IReadOnlyDictionary<string, StateSnapshot> After { get; init; }
    public required IReadOnlyList<ObjectDelta> Deltas { get; init; }
    public required IReadOnlyList<string> AffectedObjects { get; init; }
    public int PropagationDepth { get; init; }
}

public sealed record StateSnapshot
{
    public required IReadOnlyDictionary<string, object?> Properties { get; init; }
}

public sealed record ObjectDelta
{
    public required string ObjectId { get; init; }
    public required IReadOnlyList<PropertyChange> Changes { get; init; }
}

public sealed record PropertyChange
{
    public required string Key { get; init; }
    public object? Before { get; init; }
    public object? After { get; init; }
    public object? Delta { get; init; }
}
