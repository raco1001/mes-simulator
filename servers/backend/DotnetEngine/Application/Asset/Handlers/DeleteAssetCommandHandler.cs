using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Asset.Ports.Driving;

namespace DotnetEngine.Application.Asset.Handlers;

public sealed class DeleteAssetCommandHandler : IDeleteAssetCommand
{
    private readonly IAssetRepository _repository;

    public DeleteAssetCommandHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}
