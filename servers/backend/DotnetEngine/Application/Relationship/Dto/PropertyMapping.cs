namespace DotnetEngine.Application.Relationship.Dto;

/// <summary>
/// Maps a source asset property to a target property for Supplies propagation.
/// </summary>
public sealed record PropertyMapping(
    string FromProperty,
    string ToProperty,
    string TransformRule = "value",
    string? FromUnit = null,
    string? ToUnit = null);
