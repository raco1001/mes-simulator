namespace DotnetEngine.Application.Relationship.Dto;

/// <summary>
/// Relationship 수정 요청 DTO (부분 업데이트).
/// </summary>
public sealed record UpdateRelationshipRequest
{
    public string? FromAssetId { get; init; }
    public string? ToAssetId { get; init; }
    public string? RelationshipType { get; init; }
    public IReadOnlyDictionary<string, object>? Properties { get; init; }
}
