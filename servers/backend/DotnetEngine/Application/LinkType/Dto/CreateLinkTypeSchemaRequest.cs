using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.LinkType.Dto;

public sealed record CreateLinkTypeSchemaRequest
{
    public required string SchemaVersion { get; init; }
    public required string LinkType { get; init; }
    public required string DisplayName { get; init; }
    public required LinkDirection Direction { get; init; }
    public required LinkTemporality Temporality { get; init; }
    public LinkConstraint? FromConstraint { get; init; }
    public LinkConstraint? ToConstraint { get; init; }
    public IReadOnlyList<PropertyDefinition> Properties { get; init; } = [];
    public IReadOnlyList<PropertyMapping> DefaultPropertyMappings { get; init; } = [];
    public IReadOnlyList<PropertyMappingPairHint> AllowedPropertyMappingPairs { get; init; } = [];
}
