using DotnetEngine.Application.ObjectType.Dto;

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
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
