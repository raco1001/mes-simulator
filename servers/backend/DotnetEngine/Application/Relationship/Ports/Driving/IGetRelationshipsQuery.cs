using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Relationship.Ports.Driving;

/// <summary>
/// Relationship 목록/단건 조회 Port (Primary Port).
/// </summary>
public interface IGetRelationshipsQuery
{
    Task<IReadOnlyList<RelationshipDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RelationshipDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
