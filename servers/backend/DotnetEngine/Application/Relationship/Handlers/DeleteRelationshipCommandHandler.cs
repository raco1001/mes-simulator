using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Relationship.Handlers;

/// <summary>
/// Relationship 삭제 Use Case 구현 (Command Port 구현체).
/// </summary>
public sealed class DeleteRelationshipCommandHandler : IDeleteRelationshipCommand
{
    private readonly IRelationshipRepository _repository;

    public DeleteRelationshipCommandHandler(IRelationshipRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}
