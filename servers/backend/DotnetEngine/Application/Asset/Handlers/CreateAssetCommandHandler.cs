using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.Simulation;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 생성 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class CreateAssetCommandHandler : ICreateAssetCommand
{
    private readonly IAssetRepository _repository;
    private readonly IObjectTypeSchemaRepository _objectTypeSchemaRepository;

    public CreateAssetCommandHandler(
        IAssetRepository repository,
        IObjectTypeSchemaRepository objectTypeSchemaRepository)
    {
        _repository = repository;
        _objectTypeSchemaRepository = objectTypeSchemaRepository;
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

        var schema = await _objectTypeSchemaRepository.GetByObjectTypeAsync(request.Type, cancellationToken);
        var initialProperties = BuildInitialProperties(schema, asset);
        await _repository.UpsertStateAsync(new StateDto
        {
            AssetId = id,
            Properties = initialProperties,
            Status = "normal",
            LastEventType = "asset.created",
            UpdatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>()
        }, cancellationToken);

        return asset;
    }

    private static IReadOnlyDictionary<string, object?> BuildInitialProperties(
        ObjectTypeSchemaDto? schema,
        AssetDto asset)
    {
        var properties = new Dictionary<string, object?>();
        foreach (var p in EffectivePropertySetResolver.Resolve(schema, asset))
        {
            if (p.BaseValue is not null)
                properties[p.Key] = p.BaseValue;
        }
        return properties;
    }

}
