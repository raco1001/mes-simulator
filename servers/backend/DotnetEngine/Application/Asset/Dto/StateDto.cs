namespace DotnetEngine.Application.Asset.Dto;

/// <summary>
/// Asset 상태 API 응답 DTO.
/// </summary>
public sealed record StateDto
{
    public required string AssetId { get; init; }
    public double? CurrentTemp { get; init; }
    public double? CurrentPower { get; init; }
    public required string Status { get; init; }
    public string? LastEventType { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
