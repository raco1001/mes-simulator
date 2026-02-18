using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Asset.Ports;

/// <summary>
/// Asset 목록 조회 Port (Primary Port).
/// </summary>
public interface IGetAssetsQuery
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AssetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
