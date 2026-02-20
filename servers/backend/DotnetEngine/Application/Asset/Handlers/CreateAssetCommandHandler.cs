using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 생성 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class CreateAssetCommandHandler : ICreateAssetCommand
{
    private readonly IAssetRepository _repository;

    public CreateAssetCommandHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var asset = new AssetDto
        {
            Id = id,
            Type = request.Type,
            Connections = request.Connections ?? Array.Empty<string>(),
            Metadata = request.Metadata ?? new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(asset, cancellationToken);
        return asset;
    }


}
