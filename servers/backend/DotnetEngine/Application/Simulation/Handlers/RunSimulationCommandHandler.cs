using System.Collections.Generic;
using System.Linq;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation.Constants;
using DotnetEngine.Domain.Simulation.ValueObjects;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation.Rules;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.Simulation.Simulators;
using Microsoft.Extensions.Logging;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 시뮬레이션 실행 Use Case 구현. 트리거 에셋 + BFS 규칙 기반 전파, SimulationRun 세션, 이벤트 DB 저장 및 Kafka 발행.
/// </summary>
public sealed class RunSimulationCommandHandler : IRunSimulationCommand
{
    private readonly IAssetRepository _assetRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEngineStateApplier _applier;
    private readonly IEventRepository _eventRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IObjectTypeSchemaRepository _objectTypeSchemaRepository;
    private readonly IEnumerable<IPropagationRule> _rules;
    private readonly IReadOnlyDictionary<SimulationBehavior, IPropertySimulator> _simulators;
    private readonly IPropertySimulator _defaultSimulator;
    private readonly ILogger<RunSimulationCommandHandler> _logger;

    public RunSimulationCommandHandler(
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository,
        ISimulationRunRepository simulationRunRepository,
        IEngineStateApplier applier,
        IEventRepository eventRepository,
        IEventPublisher eventPublisher,
        IObjectTypeSchemaRepository objectTypeSchemaRepository,
        IEnumerable<IPropagationRule> rules,
        IEnumerable<IPropertySimulator> simulators,
        ILogger<RunSimulationCommandHandler> logger)
    {
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
        _simulationRunRepository = simulationRunRepository;
        _applier = applier;
        _eventRepository = eventRepository;
        _eventPublisher = eventPublisher;
        _objectTypeSchemaRepository = objectTypeSchemaRepository;
        _rules = rules;
        _simulators = simulators.ToDictionary(s => s.Behavior);
        _defaultSimulator = simulators.FirstOrDefault(s => s.Behavior == SimulationBehavior.Settable)
            ?? new SettableSimulator();
        _logger = logger;
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

        await RunOnePropagationAsync(runId, request, dryRun: false, cancellationToken);

        await _simulationRunRepository.EndAsync(runId, DateTimeOffset.UtcNow, cancellationToken);

        return new RunResult
        {
            Success = true,
            RunId = runId,
            Message = "Simulation run completed",
        };
    }

    public async Task<IReadOnlyDictionary<string, StateDto>> RunOnePropagationAsync(
        string runId,
        RunSimulationRequest request,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var runTick = request.RunTick;
        var maxDepth = request.MaxDepth <= 0 ? 3 : request.MaxDepth;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var cycleAccumulatedPatches = new Dictionary<string, StatePatchDto>(StringComparer.Ordinal);
        var mergedStates = new Dictionary<string, StateDto>(StringComparer.Ordinal);
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
            var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            var objectTypeSchema = asset is null
                ? null
                : await _objectTypeSchemaRepository.GetByObjectTypeAsync(asset.Type, cancellationToken);
            var mergedState = ComputeState(assetId, currentState, patch, objectTypeSchema, asset);

            var occurredAt = DateTimeOffset.UtcNow;
            var nodeEvent = new EventDto
            {
                AssetId = assetId,
                EventType = EventTypes.SimulationStateUpdated,
                OccurredAt = occurredAt,
                SimulationRunId = runId,
                RunTick = runTick,
                RelationshipId = null,
                Payload = new Dictionary<string, object>
                {
                    ["tick"] = runTick,
                    ["depth"] = depth,
                    ["status"] = mergedState.Status,
                    ["properties"] = mergedState.Properties,
                },
            };
            mergedStates[assetId] = mergedState;
            await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: dryRun, cancellationToken: cancellationToken);

            var outgoing = await _relationshipRepository.GetOutgoingAsync(assetId, cancellationToken);
            foreach (var rel in outgoing)
            {
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

                if (visited.Contains(rel.ToAssetId))
                {
                    _logger.LogWarning("Cycle detected during propagation: {FromAssetId} -> {ToAssetId} (run {RunId}, tick {Tick})", assetId, rel.ToAssetId, runId, runTick);
                    AccumulateCyclePatch(cycleAccumulatedPatches, rel.ToAssetId, nextPatch);
                }
                else
                {
                    queue.Enqueue((rel.ToAssetId, nextPatch, depth + 1));
                }

                foreach (var evt in ruleEvents)
                {
                    if (dryRun)
                        continue;
                    await _eventRepository.AppendAsync(evt, cancellationToken);
                    await _eventPublisher.PublishAsync(evt, cancellationToken);
                }
            }
        }

        foreach (var (assetId, patch) in cycleAccumulatedPatches)
        {
            if (IsConverged(patch))
                continue;

            var currentState = await _assetRepository.GetStateByAssetIdAsync(assetId, cancellationToken);
            var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            var objectTypeSchema = asset is null
                ? null
                : await _objectTypeSchemaRepository.GetByObjectTypeAsync(asset.Type, cancellationToken);
            var effectivePatch = BuildEffectiveCyclePatch(currentState, patch);
            var mergedState = ComputeState(assetId, currentState, effectivePatch, objectTypeSchema, asset);

            var nodeEvent = new EventDto
            {
                AssetId = assetId,
                EventType = EventTypes.SimulationStateUpdated,
                OccurredAt = DateTimeOffset.UtcNow,
                SimulationRunId = runId,
                RunTick = runTick,
                RelationshipId = null,
                Payload = new Dictionary<string, object>
                {
                    ["tick"] = runTick,
                    ["depth"] = maxDepth,
                    ["status"] = mergedState.Status,
                    ["properties"] = mergedState.Properties,
                    ["cycleAccumulated"] = true,
                },
            };
            mergedStates[assetId] = mergedState;
            await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: dryRun, cancellationToken: cancellationToken);
        }

        return mergedStates;
    }

    private StateDto ComputeState(
        string assetId,
        StateDto? current,
        StatePatchDto patch,
        ObjectTypeSchemaDto? objectTypeSchema,
        AssetDto? asset = null)
    {
        if (objectTypeSchema is null)
            return MergeStateFallback(assetId, current, patch);

        var now = DateTimeOffset.UtcNow;
        var currentProperties = new Dictionary<string, object?>(current?.Properties ?? new Dictionary<string, object?>());
        var mergedProperties = new Dictionary<string, object?>(currentProperties);

        var effectiveProperties = EffectivePropertySetResolver.Resolve(objectTypeSchema, asset);
        foreach (var definition in effectiveProperties)
        {
            currentProperties.TryGetValue(definition.Key, out var currentValue);
            patch.Properties.TryGetValue(definition.Key, out var patchValue);
            if (definition.Mutability == Mutability.Immutable)
                patchValue = null;

            if (!_simulators.TryGetValue(definition.SimulationBehavior, out var simulator))
            {
                _logger.LogWarning("No simulator registered for behavior {Behavior}; fallback to settable", definition.SimulationBehavior);
                simulator = _defaultSimulator;
            }

            var computed = simulator.Compute(new PropertySimulationContext
            {
                Definition = definition,
                CurrentValue = currentValue,
                PatchValue = patchValue,
                DeltaTime = TimeSpan.FromSeconds(1),
                AllProperties = mergedProperties
            });
            if (computed is null)
                mergedProperties.Remove(definition.Key);
            else
                mergedProperties[definition.Key] = computed;
        }

        foreach (var kv in patch.Properties.Where(kv => effectiveProperties.All(p => p.Key != kv.Key)))
        {
            if (kv.Value is null) mergedProperties.Remove(kv.Key);
            else mergedProperties[kv.Key] = kv.Value;
        }

        return new StateDto
        {
            AssetId = assetId,
            Properties = mergedProperties,
            Status = patch.Status ?? current?.Status ?? "normal",
            LastEventType = patch.LastEventType ?? current?.LastEventType ?? EventTypes.SimulationStateUpdated,
            UpdatedAt = now,
            Metadata = current?.Metadata ?? new Dictionary<string, object>(),
        };
    }

    private static StateDto MergeStateFallback(string assetId, StateDto? current, StatePatchDto patch)
    {
        var now = DateTimeOffset.UtcNow;
        var mergedProperties = new Dictionary<string, object?>(current?.Properties ?? new Dictionary<string, object?>());
        foreach (var kv in patch.Properties)
        {
            if (kv.Value is null)
                mergedProperties.Remove(kv.Key);
            else
                mergedProperties[kv.Key] = kv.Value;
        }

        return new StateDto
        {
            AssetId = assetId,
            Properties = mergedProperties,
            Status = patch.Status ?? current?.Status ?? "normal",
            LastEventType = patch.LastEventType ?? current?.LastEventType ?? EventTypes.SimulationStateUpdated,
            UpdatedAt = now,
            Metadata = current?.Metadata ?? new Dictionary<string, object>(),
        };
    }

    private static IReadOnlyDictionary<string, object> StatePatchToDictionary(StatePatchDto? patch)
    {
        if (patch == null)
            return new Dictionary<string, object>();

        var d = new Dictionary<string, object>(patch.Properties
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value!));
        if (patch.Status != null)
            d["status"] = patch.Status;
        if (patch.LastEventType != null)
            d["lastEventType"] = patch.LastEventType;
        return d;
    }

    private static void AccumulateCyclePatch(
        IDictionary<string, StatePatchDto> bucket,
        string assetId,
        StatePatchDto patch)
    {
        if (!bucket.TryGetValue(assetId, out var existing))
        {
            bucket[assetId] = new StatePatchDto
            {
                Properties = new Dictionary<string, object?>(patch.Properties),
                Status = patch.Status,
                LastEventType = patch.LastEventType
            };
            return;
        }

        var merged = new Dictionary<string, object?>(existing.Properties);
        foreach (var kv in patch.Properties)
        {
            if (kv.Value is null)
            {
                merged.Remove(kv.Key);
                continue;
            }

            if (merged.TryGetValue(kv.Key, out var current)
                && TryToDouble(current, out var c)
                && TryToDouble(kv.Value, out var n))
            {
                merged[kv.Key] = c + n;
            }
            else
            {
                merged[kv.Key] = kv.Value;
            }
        }

        bucket[assetId] = new StatePatchDto
        {
            Properties = merged,
            Status = patch.Status ?? existing.Status,
            LastEventType = patch.LastEventType ?? existing.LastEventType
        };
    }

    private static bool IsConverged(StatePatchDto patch)
    {
        foreach (var value in patch.Properties.Values)
        {
            if (TryToDouble(value, out var d) && Math.Abs(d) >= 0.000001d)
                return false;
            if (value is not null && !TryToDouble(value, out _))
                return false;
        }
        return true;
    }

    private static bool TryToDouble(object? value, out double number)
    {
        if (value is null)
        {
            number = 0;
            return false;
        }
        try
        {
            number = Convert.ToDouble(value);
            return true;
        }
        catch
        {
            number = 0;
            return false;
        }
    }

    private static StatePatchDto BuildEffectiveCyclePatch(StateDto? currentState, StatePatchDto accumulatedPatch)
    {
        if (currentState is null)
            return accumulatedPatch;

        var properties = new Dictionary<string, object?>(accumulatedPatch.Properties);
        foreach (var kv in accumulatedPatch.Properties)
        {
            if (!TryToDouble(kv.Value, out var patchValue))
                continue;
            if (!currentState.Properties.TryGetValue(kv.Key, out var currentObj))
                continue;
            if (!TryToDouble(currentObj, out var currentValue))
                continue;
            properties[kv.Key] = currentValue + patchValue;
        }

        return new StatePatchDto
        {
            Properties = properties,
            Status = accumulatedPatch.Status,
            LastEventType = accumulatedPatch.LastEventType
        };
    }
}
