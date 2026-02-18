using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports;
using DotnetEngine.Domain.Asset.ValueObjects;
using DotnetEngine.Infrastructure.Mongo;

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
        return states.Select(ToDto).ToList();
    }

    public async Task<StateDto?> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var state = await _repository.GetStateByAssetIdAsync(assetId, cancellationToken);
        return state == null ? null : ToDto(state);
    }

    private static StateDto ToDto(AssetState state)
    {
        return new StateDto
        {
            AssetId = state.AssetId,
            CurrentTemp = state.CurrentTemp,
            CurrentPower = state.CurrentPower,
            Status = state.Status,
            LastEventType = state.LastEventType,
            UpdatedAt = state.UpdatedAt,
            Metadata = state.Metadata
        };
    }
}
