using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Asset.Ports.Driving;

/// <summary>
/// Asset 수정 Command Port (Primary Port).
/// </summary>
public interface IUpdateAssetCommand
{
    Task<AssetDto?> UpdateAsync(string id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
}
