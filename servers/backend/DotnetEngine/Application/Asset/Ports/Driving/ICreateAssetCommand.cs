using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Asset.Ports.Driving;

/// <summary>
/// Asset 생성 Command Port (Primary Port).
/// </summary>
public interface ICreateAssetCommand
{
    Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
}
