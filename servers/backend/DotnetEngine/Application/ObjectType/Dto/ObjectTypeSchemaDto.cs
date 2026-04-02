namespace DotnetEngine.Application.ObjectType.Dto;

public sealed record PropertyDefinition
{
    public required string Key { get; init; }
    public required DataType DataType { get; init; }
    public string? Unit { get; init; }
    public required SimulationBehavior SimulationBehavior { get; init; }
    public required Mutability Mutability { get; init; }
    public object? BaseValue { get; init; }
    public IReadOnlyDictionary<string, object?> Constraints { get; init; } = new Dictionary<string, object?>();
    public bool Required { get; init; } = true;
}

public sealed record ObjectTraits
{
    public required Persistence Persistence { get; init; }
    public required Dynamism Dynamism { get; init; }
    public required Cardinality Cardinality { get; init; }
}

public sealed record Classification
{
    public required string Taxonomy { get; init; }
    public required string Value { get; init; }
}

public sealed record AllowedLink
{
    public required string LinkType { get; init; }
    public required string Direction { get; init; }
    public IReadOnlyDictionary<string, string> TargetTraits { get; init; } = new Dictionary<string, string>();
}

public sealed record ObjectTypeSchemaDto
{
    public required string SchemaVersion { get; init; }
    public required string ObjectType { get; init; }
    public required string DisplayName { get; init; }
    public bool AbstractSchema { get; init; } = false;
    public string? Extends { get; init; }
    public required ObjectTraits Traits { get; init; }
    public IReadOnlyList<Classification> Classifications { get; init; } = [];
    /// <summary>
    /// Properties defined directly on this schema (excluding inherited).
    /// Use ResolvedProperties for the full merged set when Extends is set.
    /// </summary>
    public IReadOnlyList<PropertyDefinition> OwnProperties { get; init; } = [];
    /// <summary>
    /// Runtime-merged properties: parent.OwnProperties + this.OwnProperties.
    /// Populated by the query handler; not persisted.
    /// </summary>
    public IReadOnlyList<PropertyDefinition>? ResolvedProperties { get; init; }
    public IReadOnlyList<AllowedLink> AllowedLinks { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
