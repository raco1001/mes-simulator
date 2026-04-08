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
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StateDto>> GetAllStatesAsync(CancellationToken cancellationToken = default);
    Task<StateDto?> GetStateByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);
    Task UpsertStateAsync(StateDto state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes persisted simulation rows for the given asset ids (no-op if list is empty).
    /// </summary>
    Task DeleteStatesByAssetIdsAsync(IReadOnlyList<string> assetIds, CancellationToken cancellationToken = default);
}
