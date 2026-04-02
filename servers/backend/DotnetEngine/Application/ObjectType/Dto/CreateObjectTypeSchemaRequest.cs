namespace DotnetEngine.Application.ObjectType.Dto;

public sealed record CreateObjectTypeSchemaRequest
{
    public required string SchemaVersion { get; init; }
    public required string ObjectType { get; init; }
    public required string DisplayName { get; init; }
    public required ObjectTraits Traits { get; init; }
    public IReadOnlyList<Classification> Classifications { get; init; } = [];
    public IReadOnlyList<PropertyDefinition> OwnProperties { get; init; } = [];
    public IReadOnlyList<AllowedLink> AllowedLinks { get; init; } = [];
}
