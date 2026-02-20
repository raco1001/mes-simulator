namespace DotnetEngine.Application.Relationship.Ports.Driving;

/// <summary>
/// Relationship 삭제 Command Port (Primary Port).
/// </summary>
public interface IDeleteRelationshipCommand
{
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
