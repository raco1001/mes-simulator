using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Relationship.Handlers;

/// <summary>
/// Relationship 수정 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class UpdateRelationshipCommandHandler : IUpdateRelationshipCommand
{
    private readonly IRelationshipRepository _repository;
    private readonly ILinkTypeSchemaRepository _linkTypeSchemaRepository;
    private readonly IRelationshipPropertyMappingValidator _mappingValidator;

    public UpdateRelationshipCommandHandler(
        IRelationshipRepository repository,
        ILinkTypeSchemaRepository linkTypeSchemaRepository,
        IRelationshipPropertyMappingValidator mappingValidator)
    {
        _repository = repository;
        _linkTypeSchemaRepository = linkTypeSchemaRepository;
        _mappingValidator = mappingValidator;
    }

    public async Task<RelationshipDto?> UpdateAsync(string id, UpdateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new RelationshipDto
        {
            Id = existing.Id,
            FromAssetId = request.FromAssetId ?? existing.FromAssetId,
            ToAssetId = request.ToAssetId ?? existing.ToAssetId,
            RelationshipType = request.RelationshipType ?? existing.RelationshipType,
            Properties = request.Properties ?? existing.Properties,
            Mappings = request.Mappings ?? existing.Mappings,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var linkSchema = await _linkTypeSchemaRepository.GetByLinkTypeAsync(updated.RelationshipType, cancellationToken);
        if (updated.Mappings.Count > 0)
        {
            await _mappingValidator.ValidateAsync(
                updated.Mappings,
                updated.FromAssetId,
                updated.ToAssetId,
                linkSchema,
                cancellationToken);
        }

        return await _repository.UpdateAsync(id, updated, cancellationToken);
    }
}
