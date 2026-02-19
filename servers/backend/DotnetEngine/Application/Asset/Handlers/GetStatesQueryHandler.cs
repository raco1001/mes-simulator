using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;

namespace DotnetEngine.Application.Asset.Handlers;

/// <summary>
/// Asset 상태 조회 Use Case 구현 (Port 구현체).
/// </summary>
public sealed class GetStatesQueryHandler : IGetStatesQuery
{
    private readonly IAssetRepository _repository;

    public GetStatesQueryHandler(IAssetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var states = await _repository.GetAllStatesAsync(cancellationToken);
        if (states.Count == 0)
        {
            return new List<StateDto>();
        }
        return states;
    }

    public async Task<StateDto?> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var state = await _repository.GetStateByAssetIdAsync(assetId, cancellationToken);
        if (state is null)
        {
            return null;
        }
        return state;
    }
}
