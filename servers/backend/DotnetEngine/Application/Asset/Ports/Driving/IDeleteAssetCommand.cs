using DotnetEngine.Application.Asset;

namespace DotnetEngine.Application.Asset.Ports.Driving;

public interface IDeleteAssetCommand
{
    Task<DeleteAssetResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
