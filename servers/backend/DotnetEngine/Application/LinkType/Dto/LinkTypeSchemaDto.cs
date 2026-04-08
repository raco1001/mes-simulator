using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.LinkType.Dto;

public sealed record LinkTypeSchemaDto
{
    public required string SchemaVersion { get; init; }
    public required string LinkType { get; init; }
    public required string DisplayName { get; init; }
    public required LinkDirection Direction { get; init; }
    public required LinkTemporality Temporality { get; init; }
    public LinkConstraint? FromConstraint { get; init; }
    public LinkConstraint? ToConstraint { get; init; }
    public IReadOnlyList<PropertyDefinition> Properties { get; init; } = [];
    /// <summary>When creating a relationship with empty mappings, these are used as initial <see cref="RelationshipDto.Mappings"/>.</summary>
    public IReadOnlyList<PropertyMapping> DefaultPropertyMappings { get; init; } = [];
    /// <summary>If non-empty, each mapping must match one of these (from→to) pairs.</summary>
    public IReadOnlyList<PropertyMappingPairHint> AllowedPropertyMappingPairs { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
