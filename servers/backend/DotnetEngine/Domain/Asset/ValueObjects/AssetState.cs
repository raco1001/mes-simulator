namespace DotnetEngine.Domain.Asset.ValueObjects;

/// <summary>
/// Asset 상태 Value Object (MongoDB에서 조회한 데이터를 표현).
/// </summary>
public sealed record AssetState
{
    public required string AssetId { get; init; }
    public double? CurrentTemp { get; init; }
    public double? CurrentPower { get; init; }
    public required string Status { get; init; }
    public string? LastEventType { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
