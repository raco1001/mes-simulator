using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Relationship.Ports.Driven;

/// <summary>
/// Relationship Repository 인터페이스 (Port - Secondary/Driven).
/// </summary>
public interface IRelationshipRepository
{
    Task<IReadOnlyList<RelationshipDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RelationshipDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<RelationshipDto> AddAsync(RelationshipDto dto, CancellationToken cancellationToken = default);
    Task<RelationshipDto?> UpdateAsync(string id, RelationshipDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
