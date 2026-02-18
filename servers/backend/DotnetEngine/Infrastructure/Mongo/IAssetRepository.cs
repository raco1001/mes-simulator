using DotnetEngine.Domain.Asset.ValueObjects;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// Asset Repository 인터페이스 (Port - Secondary/Driven).
/// </summary>
public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Asset?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetState>> GetAllStatesAsync(CancellationToken cancellationToken = default);
    Task<AssetState?> GetStateByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);
}
