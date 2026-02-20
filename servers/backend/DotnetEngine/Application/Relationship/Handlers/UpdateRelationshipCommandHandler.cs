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

    public UpdateRelationshipCommandHandler(IRelationshipRepository repository)
    {
        _repository = repository;
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
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.UpdateAsync(id, updated, cancellationToken);
    }
}
