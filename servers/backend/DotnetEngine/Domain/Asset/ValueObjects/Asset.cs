namespace DotnetEngine.Domain.Asset.ValueObjects;

/// <summary>
/// Asset 엔티티 (MongoDB에서 조회한 데이터를 표현).
/// </summary>
public sealed record Asset
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public IReadOnlyList<string> Connections { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
