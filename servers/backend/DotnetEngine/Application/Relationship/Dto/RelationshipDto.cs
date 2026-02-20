namespace DotnetEngine.Application.Relationship.Dto;

/// <summary>
/// Relationship API 응답 DTO.
/// </summary>
public sealed record RelationshipDto
{
    public required string Id { get; init; }
    public required string FromAssetId { get; init; }
    public required string ToAssetId { get; init; }
    public required string RelationshipType { get; init; }
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
