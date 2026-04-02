namespace DotnetEngine.Application.Asset.Ports.Driving;

public interface IDeleteAssetCommand
{
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
