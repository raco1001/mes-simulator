using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Relationship.Ports.Driving;

/// <summary>
/// Relationship 생성 Command Port (Primary Port).
/// </summary>
public interface ICreateRelationshipCommand
{
    Task<RelationshipDto> CreateAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default);
}
