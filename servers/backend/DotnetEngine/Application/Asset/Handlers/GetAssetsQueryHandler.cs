using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 목록 조회 Use Case 구현 (Port 구현체).
/// </summary>
public sealed class GetAssetsQueryHandler : IGetAssetsQuery
{
    private readonly IAssetRepository _repository;

    public  GetAssetsQueryHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _repository.GetAllAsync(cancellationToken);
        if (assets.Count == 0)
        {
            return new List<AssetDto>();
        }
        return assets;
    }

    public async Task<AssetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var asset = await _repository.GetByIdAsync(id, cancellationToken);
        return asset;
    }
}
