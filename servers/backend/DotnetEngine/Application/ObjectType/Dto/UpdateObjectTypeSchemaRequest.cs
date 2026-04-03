namespace DotnetEngine.Application.ObjectType.Dto;

public sealed record UpdateObjectTypeSchemaRequest
{
    public string? SchemaVersion { get; init; }
    public string? DisplayName { get; init; }
    public ObjectTraits? Traits { get; init; }
    public IReadOnlyList<Classification>? Classifications { get; init; }
    public IReadOnlyList<PropertyDefinition>? OwnProperties { get; init; }
    public IReadOnlyList<AllowedLink>? AllowedLinks { get; init; }
}
