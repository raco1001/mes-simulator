using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Asset.Ports;

/// <summary>
/// Asset 상태 조회 Port (Primary Port).
/// </summary>
public interface IGetStatesQuery
{
    Task<IReadOnlyList<StateDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StateDto?> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);
}
