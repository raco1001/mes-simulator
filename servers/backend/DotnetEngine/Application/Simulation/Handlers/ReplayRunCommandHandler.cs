using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Domain.Simulation.Constants;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// Replay Use Case: runId(·tickMax) 기준 이벤트 조회 후 상태만 Upsert (재기록/재발행 없음).
/// </summary>
public sealed class ReplayRunCommandHandler : IReplayRunCommand
{
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IAssetRepository _assetRepository;

    public ReplayRunCommandHandler(
        ISimulationRunRepository simulationRunRepository,
        IEventRepository eventRepository,
        IAssetRepository assetRepository)
    {
        _simulationRunRepository = simulationRunRepository;
        _eventRepository = eventRepository;
        _assetRepository = assetRepository;
    }

    public async Task<ReplayRunResult> ReplayAsync(string runId, int? tickMax, CancellationToken cancellationToken = default)
    {
        var run = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return new ReplayRunResult { Success = false, RunId = runId, TickMax = tickMax, Message = "Run not found" };

        var events = await _eventRepository.GetBySimulationRunIdAsync(runId, tickMax, cancellationToken);
        var applied = 0;
        foreach (var evt in events)
        {
            if (evt.EventType != EventTypes.SimulationStateUpdated)
                continue;
            var state = EventToStateDto(evt);
            if (state == null)
                continue;
            await _assetRepository.UpsertStateAsync(state, cancellationToken);
            applied++;
        }

        return new ReplayRunResult
        {
            Success = true,
            RunId = runId,
            TickMax = tickMax,
            EventsApplied = applied,
            Message = $"Replayed {applied} state updates.",
        };
    }

    private static StateDto? EventToStateDto(EventDto evt)
    {
        var p = evt.Payload;
        var status = GetString(p, "status") ?? "normal";
        double? temp = GetDouble(p, "temperature");
        double? power = GetDouble(p, "power");
        return new StateDto
        {
            AssetId = evt.AssetId,
            CurrentTemp = temp,
            CurrentPower = power,
            Status = status,
            LastEventType = evt.EventType,
            UpdatedAt = evt.OccurredAt,
            Metadata = new Dictionary<string, object>(),
        };
    }

    private static string? GetString(IReadOnlyDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v))
            return null;
        return v?.ToString();
    }

    private static double? GetDouble(IReadOnlyDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v))
            return null;
        return v switch
        {
            double d => d,
            int i => i,
            long l => l,
            _ => null,
        };
    }
}
