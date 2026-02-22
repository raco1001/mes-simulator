using System.Collections.Generic;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation.Rules;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 시뮬레이션 실행 Use Case 구현. 트리거 에셋 + BFS 규칙 기반 전파, SimulationRun 세션, 이벤트 DB 저장 및 Kafka 발행.
/// </summary>
public sealed class RunSimulationCommandHandler : IRunSimulationCommand
{
    private const string EventTypeStateUpdated = "simulation.state.updated";

    private readonly IAssetRepository _assetRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEnumerable<IPropagationRule> _rules;

    public RunSimulationCommandHandler(
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository,
        ISimulationRunRepository simulationRunRepository,
        IEventRepository eventRepository,
        IEventPublisher eventPublisher,
        IEnumerable<IPropagationRule> rules)
    {
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
        _simulationRunRepository = simulationRunRepository;
        _eventRepository = eventRepository;
        _eventPublisher = eventPublisher;
        _rules = rules;
    }

    public async Task<RunResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString("N");
        var maxDepth = request.MaxDepth <= 0 ? 3 : request.MaxDepth;
        var startedAt = DateTimeOffset.UtcNow;
        var triggerDict = StatePatchToDictionary(request.Patch);

        var runDto = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Pending,
            StartedAt = startedAt,
            EndedAt = null,
            TriggerAssetId = request.TriggerAssetId,
            Trigger = triggerDict,
            MaxDepth = maxDepth,
            TickIndex = 0,
        };
        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);
        await _simulationRunRepository.UpdateStatusAsync(runId, SimulationRunStatus.Running, null, cancellationToken);

        await RunOnePropagationAsync(runId, request, cancellationToken);

        await _simulationRunRepository.EndAsync(runId, DateTimeOffset.UtcNow, cancellationToken);

        return new RunResult
        {
            Success = true,
            RunId = runId,
            Message = "Simulation run completed",
        };
    }

    public async Task RunOnePropagationAsync(string runId, RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var runTick = request.RunTick;
        var maxDepth = request.MaxDepth <= 0 ? 3 : request.MaxDepth;
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
            var nodeEvent = new EventDto
            {
                AssetId = assetId,
                EventType = EventTypeStateUpdated,
                OccurredAt = occurredAt,
                SimulationRunId = runId,
                RelationshipId = null,
                Payload = new Dictionary<string, object>
                {
                    ["tick"] = runTick,
                    ["depth"] = depth,
                    ["status"] = mergedState.Status,
                    ["temperature"] = mergedState.CurrentTemp ?? 0d,
                    ["power"] = mergedState.CurrentPower ?? 0d,
                },
            };
            await _eventRepository.AppendAsync(nodeEvent, cancellationToken);
            await _eventPublisher.PublishAsync(nodeEvent, cancellationToken);

            var outgoing = await _relationshipRepository.GetOutgoingAsync(assetId, cancellationToken);
            foreach (var rel in outgoing)
            {
                if (visited.Contains(rel.ToAssetId))
                    continue;

                var ctx = new PropagationContext
                {
                    FromAssetId = assetId,
                    FromState = mergedState,
                    Relationship = rel,
                    ToAssetId = rel.ToAssetId,
                    ToState = null,
                    IncomingPatch = patch,
                    Depth = depth + 1,
                    SimulationRunId = runId,
                    RunTick = runTick,
                };

                IPropagationRule? appliedRule = null;
                foreach (var rule in _rules)
                {
                    if (rule.CanApply(ctx))
                    {
                        appliedRule = rule;
                        break;
                    }
                }

                StatePatchDto nextPatch;
                IReadOnlyList<EventDto> ruleEvents;
                if (appliedRule != null)
                {
                    var result = appliedRule.Apply(ctx);
                    nextPatch = result.OutgoingPatch;
                    ruleEvents = result.Events;
                }
                else
                {
                    nextPatch = patch;
                    ruleEvents = [];
                }

                queue.Enqueue((rel.ToAssetId, nextPatch, depth + 1));

                foreach (var evt in ruleEvents)
                {
                    await _eventRepository.AppendAsync(evt, cancellationToken);
                    await _eventPublisher.PublishAsync(evt, cancellationToken);
                }
            }
        }
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
