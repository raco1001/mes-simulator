namespace DotnetEngine.Application.Asset.Dto;

/// <summary>
/// Asset 상태 API 응답 DTO.
/// </summary>
public sealed record StateDto
{
    public required string AssetId { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    public required string Status { get; init; }
    public string? LastEventType { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
