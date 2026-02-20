using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Relationship.Handlers;

/// <summary>
/// Relationship 목록/단건 조회 Use Case 구현 (Port 구현체).
/// </summary>
public sealed class GetRelationshipsQueryHandler : IGetRelationshipsQuery
{
    private readonly IRelationshipRepository _repository;

    public GetRelationshipsQueryHandler(IRelationshipRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RelationshipDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Count == 0 ? new List<RelationshipDto>() : list;
    }

    public async Task<RelationshipDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }
}
