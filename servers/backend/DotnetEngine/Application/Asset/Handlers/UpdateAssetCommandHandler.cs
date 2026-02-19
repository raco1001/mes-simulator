using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using DomainAsset = DotnetEngine.Domain.Asset.Entities.Asset;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 수정 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class UpdateAssetCommandHandler : IUpdateAssetCommand
{
    private readonly IAssetRepository _repository;

    public UpdateAssetCommandHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssetDto?> UpdateAsync(string id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new AssetDto
        {
            Id = existing.Id,
            Type = request.Type ?? existing.Type,
            Connections = request.Connections ?? existing.Connections,
            Metadata = request.Metadata ?? existing.Metadata,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.UpdateAsync(id, updated, cancellationToken);
    }
}
