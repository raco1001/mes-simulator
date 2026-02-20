using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Relationship.Ports.Driving;

/// <summary>
/// Relationship 수정 Command Port (Primary Port).
/// </summary>
public interface IUpdateRelationshipCommand
{
    Task<RelationshipDto?> UpdateAsync(string id, UpdateRelationshipRequest request, CancellationToken cancellationToken = default);
}
