namespace DotnetEngine.Application.Asset.Dto;

/// <summary>
/// Asset API 응답 DTO.
/// </summary>
public sealed record AssetDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public IReadOnlyList<string> Connections { get; init; } = [];
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
