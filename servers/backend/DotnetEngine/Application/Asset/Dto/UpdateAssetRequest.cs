namespace DotnetEngine.Application.Asset.Dto;

/// <summary>
/// Asset 수정 요청 DTO (부분 업데이트).
/// </summary>
public sealed record UpdateAssetRequest
{
    public string? Type { get; init; }
    public IReadOnlyList<string>? Connections { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
