using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// 상태 변경 진입점 구현. UpsertState → Append → Publish 순서로 apply 수행.
/// </summary>
public sealed class EngineStateApplier : IEngineStateApplier
{
    private readonly IAssetRepository _assetRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventPublisher _eventPublisher;

    public EngineStateApplier(
        IAssetRepository assetRepository,
        IEventRepository eventRepository,
        IEventPublisher eventPublisher)
    {
        _assetRepository = assetRepository;
        _eventRepository = eventRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task ApplyAsync(EventDto evt, StateDto mergedState, CancellationToken cancellationToken = default)
    {
        await _assetRepository.UpsertStateAsync(mergedState, cancellationToken);
        await _eventRepository.AppendAsync(evt, cancellationToken);
        await _eventPublisher.PublishAsync(evt, cancellationToken);
    }
}
