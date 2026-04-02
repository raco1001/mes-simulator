using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.LinkType.Dto;

public sealed record LinkConstraint
{
    public ObjectTraits? RequiredTraits { get; init; }
    public IReadOnlyList<string>? AllowedObjectTypes { get; init; }
}
