using System.Collections.Generic;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 시뮬레이션 실행 Use Case 구현. 트리거 에셋 + BFS 1회 전파, SimulationRun 세션 및 이벤트 기록.
/// </summary>
public sealed class RunSimulationCommandHandler : IRunSimulationCommand
{
    private const string EventTypeStateUpdated = "simulation.state.updated";

    private readonly IAssetRepository _assetRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEventRepository _eventRepository;

    public RunSimulationCommandHandler(
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository,
        ISimulationRunRepository simulationRunRepository,
        IEventRepository eventRepository)
    {
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
        _simulationRunRepository = simulationRunRepository;
        _eventRepository = eventRepository;
    }

    public async Task<RunResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var maxDepth = request.MaxDepth <= 0 ? 3 : request.MaxDepth;
        var runId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var triggerDict = StatePatchToDictionary(request.Patch);

        var runDto = new SimulationRunDto
        {
            Id = runId,
            StartedAt = startedAt,
            EndedAt = null,
            TriggerAssetId = request.TriggerAssetId,
            Trigger = triggerDict,
            MaxDepth = maxDepth,
        };
        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string AssetId, StatePatchDto Patch, int Depth)>();
        queue.Enqueue((request.TriggerAssetId, request.Patch ?? new StatePatchDto(), 0));

        while (queue.Count > 0)
        {
            var (assetId, patch, depth) = queue.Dequeue();
            if (depth > maxDepth)
                continue;
            if (visited.Contains(assetId))
                continue;
            visited.Add(assetId);

            var currentState = await _assetRepository.GetStateByAssetIdAsync(assetId, cancellationToken);
            var mergedState = MergeState(assetId, currentState, patch);
            await _assetRepository.UpsertStateAsync(mergedState, cancellationToken);

            var occurredAt = DateTimeOffset.UtcNow;
            await _eventRepository.AppendAsync(new EventDto
            {
                AssetId = assetId,
                EventType = EventTypeStateUpdated,
                OccurredAt = occurredAt,
                SimulationRunId = runId,
                RelationshipId = null,
                Payload = new Dictionary<string, object>
                {
                    ["depth"] = depth,
                    ["status"] = mergedState.Status,
                },
            }, cancellationToken);

            var outgoing = await _relationshipRepository.GetOutgoingAsync(assetId, cancellationToken);
            foreach (RelationshipDto rel in outgoing)
            {
                if (!visited.Contains(rel.ToAssetId))
                    queue.Enqueue((rel.ToAssetId, patch, depth + 1));
            }
        }

        await _simulationRunRepository.EndAsync(runId, DateTimeOffset.UtcNow, cancellationToken);

        return new RunResult
        {
            Success = true,
            RunId = runId,
            Message = "Simulation run completed",
        };
    }

    private static StateDto MergeState(string assetId, StateDto? current, StatePatchDto patch)
    {
        var now = DateTimeOffset.UtcNow;
        return new StateDto
        {
            AssetId = assetId,
            CurrentTemp = patch.CurrentTemp ?? current?.CurrentTemp,
            CurrentPower = patch.CurrentPower ?? current?.CurrentPower,
            Status = patch.Status ?? current?.Status ?? "normal",
            LastEventType = patch.LastEventType ?? current?.LastEventType ?? EventTypeStateUpdated,
            UpdatedAt = now,
            Metadata = current?.Metadata ?? new Dictionary<string, object>(),
        };
    }

    private static IReadOnlyDictionary<string, object> StatePatchToDictionary(StatePatchDto? patch)
    {
        if (patch == null)
            return new Dictionary<string, object>();

        var d = new Dictionary<string, object>();
        if (patch.CurrentTemp.HasValue)
            d["currentTemp"] = patch.CurrentTemp.Value;
        if (patch.CurrentPower.HasValue)
            d["currentPower"] = patch.CurrentPower.Value;
        if (patch.Status != null)
            d["status"] = patch.Status;
        if (patch.LastEventType != null)
            d["lastEventType"] = patch.LastEventType;
        return d;
    }
}
