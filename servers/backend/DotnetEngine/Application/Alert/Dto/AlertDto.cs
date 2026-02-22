namespace DotnetEngine.Application.Alert.Dto;

/// <summary>
/// Alert API 응답 DTO (alert.generated 이벤트).
/// </summary>
public sealed record AlertDto
{
    public required string AssetId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public string? RunId { get; init; }
    public string? Metric { get; init; }
    public double? Current { get; init; }
    public double? Threshold { get; init; }
    public string? Code { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
