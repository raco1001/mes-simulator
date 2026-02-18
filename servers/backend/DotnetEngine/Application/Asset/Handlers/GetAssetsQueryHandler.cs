using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports;
using DotnetEngine.Domain.Asset.ValueObjects;
using DotnetEngine.Infrastructure.Mongo;
using DomainAsset = DotnetEngine.Domain.Asset.ValueObjects.Asset;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 목록 조회 Use Case 구현 (Port 구현체).
/// </summary>
public sealed class GetAssetsQueryHandler : IGetAssetsQuery
{
    private readonly IAssetRepository _repository;

    public GetAssetsQueryHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _repository.GetAllAsync(cancellationToken);
        return assets.Select(ToDto).ToList();
    }

    public async Task<AssetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var asset = await _repository.GetByIdAsync(id, cancellationToken);
        return asset == null ? null : ToDto(asset);
    }

    private static AssetDto ToDto(DomainAsset asset)
    {
        return new AssetDto
        {
            Id = asset.Id,
            Type = asset.Type,
            Connections = asset.Connections,
            Metadata = asset.Metadata,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }
}
