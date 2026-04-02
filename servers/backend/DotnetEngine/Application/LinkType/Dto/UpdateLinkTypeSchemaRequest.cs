using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.LinkType.Dto;

public sealed record UpdateLinkTypeSchemaRequest
{
    public string? SchemaVersion { get; init; }
    public string? DisplayName { get; init; }
    public LinkDirection? Direction { get; init; }
    public LinkTemporality? Temporality { get; init; }
    public LinkConstraint? FromConstraint { get; init; }
    public LinkConstraint? ToConstraint { get; init; }
    public IReadOnlyList<PropertyDefinition>? Properties { get; init; }
}
