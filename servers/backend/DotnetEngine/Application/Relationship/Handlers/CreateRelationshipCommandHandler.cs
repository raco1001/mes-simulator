using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Relationship.Handlers;

/// <summary>
/// Relationship 생성 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class CreateRelationshipCommandHandler : ICreateRelationshipCommand
{
    private readonly IRelationshipRepository _repository;

    public CreateRelationshipCommandHandler(IRelationshipRepository repository)
    {
        _repository = repository;
    }

    public async Task<RelationshipDto> CreateAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var dto = new RelationshipDto
        {
            Id = id,
            FromAssetId = request.FromAssetId,
            ToAssetId = request.ToAssetId,
            RelationshipType = request.RelationshipType,
            Properties = request.Properties ?? new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(dto, cancellationToken);
        return dto;
    }
}
