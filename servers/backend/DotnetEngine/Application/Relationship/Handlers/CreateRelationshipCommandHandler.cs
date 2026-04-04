using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.ObjectType.Dto;
using Microsoft.Extensions.Logging;

namespace DotnetEngine.Application.Relationship.Handlers;

/// <summary>
/// Relationship 생성 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class CreateRelationshipCommandHandler : ICreateRelationshipCommand
{
    private readonly IRelationshipRepository _repository;
    private readonly IAssetRepository? _assetRepository;
    private readonly IObjectTypeSchemaRepository? _objectTypeSchemaRepository;
    private readonly ILinkTypeSchemaRepository? _linkTypeSchemaRepository;
    private readonly ILogger<CreateRelationshipCommandHandler>? _logger;

    public CreateRelationshipCommandHandler(IRelationshipRepository repository)
        : this(repository, null, null, null, null)
    {
    }

    public CreateRelationshipCommandHandler(
        IRelationshipRepository repository,
        IAssetRepository? assetRepository,
        IObjectTypeSchemaRepository? objectTypeSchemaRepository,
        ILinkTypeSchemaRepository? linkTypeSchemaRepository,
        ILogger<CreateRelationshipCommandHandler>? logger)
    {
        _repository = repository;
        _assetRepository = assetRepository;
        _objectTypeSchemaRepository = objectTypeSchemaRepository;
        _linkTypeSchemaRepository = linkTypeSchemaRepository;
        _logger = logger;
    }

    public async Task<RelationshipDto> CreateAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        var seededProperties = await BuildSeededPropertiesAsync(request, cancellationToken);
        await ValidateConstraintsWithWarningAsync(request, cancellationToken);

        var id = Guid.NewGuid().ToString("N");
        var dto = new RelationshipDto
        {
            Id = id,
            FromAssetId = request.FromAssetId,
            ToAssetId = request.ToAssetId,
            RelationshipType = request.RelationshipType,
            Properties = seededProperties,
            Mappings = request.Mappings,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(dto, cancellationToken);
        return dto;
    }

    private async Task<IReadOnlyDictionary<string, object>> BuildSeededPropertiesAsync(
        CreateRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object>();
        if (_linkTypeSchemaRepository != null)
        {
            var schema = await _linkTypeSchemaRepository.GetByLinkTypeAsync(request.RelationshipType, cancellationToken);
            if (schema != null)
            {
                foreach (var property in schema.Properties)
                {
                    if (property.BaseValue != null)
                        result[property.Key] = property.BaseValue;
                }
            }
        }

        foreach (var kv in request.Properties)
            result[kv.Key] = kv.Value;
        return result;
    }

    private async Task ValidateConstraintsWithWarningAsync(CreateRelationshipRequest request, CancellationToken cancellationToken)
    {
        if (_assetRepository is null || _objectTypeSchemaRepository is null || _linkTypeSchemaRepository is null || _logger is null)
            return;

        var linkSchema = await _linkTypeSchemaRepository.GetByLinkTypeAsync(request.RelationshipType, cancellationToken);
        if (linkSchema is null)
            return;

        var fromAsset = await _assetRepository.GetByIdAsync(request.FromAssetId, cancellationToken);
        var toAsset = await _assetRepository.GetByIdAsync(request.ToAssetId, cancellationToken);
        if (fromAsset is null || toAsset is null)
            return;

        var fromObjectSchema = await _objectTypeSchemaRepository.GetByObjectTypeAsync(fromAsset.Type, cancellationToken);
        var toObjectSchema = await _objectTypeSchemaRepository.GetByObjectTypeAsync(toAsset.Type, cancellationToken);
        if (fromObjectSchema is null || toObjectSchema is null)
            return;

        if (!ConstraintSatisfied(linkSchema.FromConstraint, fromObjectSchema))
            _logger.LogWarning("LinkTypeSchema constraint mismatch (from): linkType={LinkType}, fromAsset={FromAssetId}", request.RelationshipType, request.FromAssetId);
        if (!ConstraintSatisfied(linkSchema.ToConstraint, toObjectSchema))
            _logger.LogWarning("LinkTypeSchema constraint mismatch (to): linkType={LinkType}, toAsset={ToAssetId}", request.RelationshipType, request.ToAssetId);
    }

    private static bool ConstraintSatisfied(LinkConstraint? constraint, ObjectTypeSchemaDto objectSchema)
    {
        if (constraint is null)
            return true;

        if (constraint.AllowedObjectTypes is { Count: > 0 } &&
            !constraint.AllowedObjectTypes.Contains(objectSchema.ObjectType, StringComparer.OrdinalIgnoreCase))
            return false;

        if (constraint.RequiredTraits is null)
            return true;

        var required = constraint.RequiredTraits;
        var actual = objectSchema.Traits;
        return required.Persistence == actual.Persistence
            && required.Dynamism == actual.Dynamism
            && required.Cardinality == actual.Cardinality;
    }
}
