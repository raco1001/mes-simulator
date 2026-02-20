namespace DotnetEngine.Application.Relationship.Dto;

/// <summary>
/// Relationship 생성 요청 DTO.
/// </summary>
public sealed record CreateRelationshipRequest
{
    public required string FromAssetId { get; init; }
    public required string ToAssetId { get; init; }
    public required string RelationshipType { get; init; }
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}
