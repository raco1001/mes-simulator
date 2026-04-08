using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Asset.Handlers;

public sealed class DeleteAssetCommandHandler : IDeleteAssetCommand
{
    private readonly IAssetRepository _repository;
    private readonly IRelationshipRepository _relationshipRepository;

    public DeleteAssetCommandHandler(
        IAssetRepository repository,
        IRelationshipRepository relationshipRepository)
    {
        _repository = repository;
        _relationshipRepository = relationshipRepository;
    }

    public async Task<DeleteAssetResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var asset = await _repository.GetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return DeleteAssetResult.NotFound;
        }

        if (await _relationshipRepository.ExistsForAssetAsync(id, cancellationToken))
        {
            return DeleteAssetResult.HasRelationships;
        }

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted ? DeleteAssetResult.Deleted : DeleteAssetResult.NotFound;
    }
}
