using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Asset.Ports.Driven;

/// <summary>
/// Asset Repository 인터페이스 (Port - Secondary/Driven).
/// </summary>
public interface IAssetRepository
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AssetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AssetDto> AddAsync(AssetDto assetDto, CancellationToken cancellationToken = default);
    Task<AssetDto?> UpdateAsync(string id, AssetDto assetDto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StateDto>> GetAllStatesAsync(CancellationToken cancellationToken = default);
    Task<StateDto?> GetStateByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);
}
