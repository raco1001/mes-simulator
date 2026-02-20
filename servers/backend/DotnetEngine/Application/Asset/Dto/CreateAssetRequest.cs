namespace DotnetEngine.Application.Asset.Dto;

/// <summary>
/// Asset 생성 요청 DTO.
/// </summary>
public sealed record CreateAssetRequest
{
    public required string Type { get; init; }
    public IReadOnlyList<string> Connections { get; init; } = [];
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
