using System.Collections.Generic;
using System.Linq;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation;
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
        var triggerIds = request.ResolveTriggerAssetIds();
        if (triggerIds.Count == 0)
            return new RunResult
            {
                Success = false,
                RunId = "",
                Message = "triggerAssetIds (non-empty) or triggerAssetId is required.",
            };
        if (RunSimulationRequestExtensions.IsMultiSeedPatchDisallowed(triggerIds, request.Patch))
            return new RunResult
            {
                Success = false,
                RunId = "",
                Message = "Multiple trigger seeds cannot be combined with a non-empty patch.",
            };

        var runId = Guid.NewGuid().ToString("N");
        var maxDepth = ResolveMaxDepth(request.MaxDepth);
        var startedAt = DateTimeOffset.UtcNow;
        var triggerDict = StatePatchToDictionary(request.Patch);
        var engineTick = SimulationEngineConstants.ClampEngineTickIntervalMs(request.EngineTickIntervalMs);

        var runDto = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Pending,
            StartedAt = startedAt,
            EndedAt = null,
            TriggerAssetIds = triggerIds.ToList(),
            Trigger = triggerDict,
            MaxDepth = maxDepth,
            EngineTickIntervalMs = engineTick,
            TickIndex = 0,
        };
        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);
        await _simulationRunRepository.UpdateStatusAsync(runId, SimulationRunStatus.Running, null, cancellationToken);

        await SimulationPersistedStateReset.ApplyIfNeededAsync(
            request,
            dryRun: false,
            _relationshipRepository,
            _assetRepository,
            cancellationToken);

        await RunOnePropagationAsync(runId, request, dryRun: false, cancellationToken);

        await _simulationRunRepository.EndAsync(runId, DateTimeOffset.UtcNow, cancellationToken);

        return new RunResult
        {
            Success = true,
            RunId = runId,
            Message = "Simulation run completed",
        };
    }

    public async Task<RunPropagationOutcome> RunOnePropagationAsync(
        string runId,
        RunSimulationRequest request,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var runTick = request.RunTick;
        var maxDepth = ResolveMaxDepth(request.MaxDepth);
        var runRecord = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var firstVisitPatchedKeysByAsset = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var parent = new Dictionary<string, string?>(StringComparer.Ordinal);
        var cycleAccumulatedPatches = new Dictionary<string, StatePatchDto>(StringComparer.Ordinal);
        var mergedStates = new Dictionary<string, StateDto>(StringComparer.Ordinal);
        var changedAssetIds = new List<string>();
        var queue = new Queue<(string AssetId, StatePatchDto Patch, int Depth, string? FromAssetId)>();
        var seeds = request.ResolveTriggerAssetIds();
        var initialPatch = request.Patch ?? new StatePatchDto();
        foreach (var seed in seeds)
            queue.Enqueue((seed, initialPatch, 0, null));

        while (queue.Count > 0)
        {
            var (assetId, patch, depth, fromAssetId) = queue.Dequeue();
            if (depth > maxDepth)
                continue;

            var isFanIn = visited.Contains(assetId);
            if (!isFanIn)
            {
                visited.Add(assetId);
                if (!parent.ContainsKey(assetId))
                    parent[assetId] = fromAssetId;
                firstVisitPatchedKeysByAsset[assetId] = new HashSet<string>(patch.Properties.Keys, StringComparer.Ordinal);
            }

            firstVisitPatchedKeysByAsset.TryGetValue(assetId, out var firstVisitKeysForAsset);

            var currentState = await _assetRepository.GetStateByAssetIdAsync(assetId, cancellationToken);
            if (request.ResetState && dryRun)
                currentState = null;
            var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            var objectTypeSchema = asset is null
                ? null
                : await _objectTypeSchemaRepository.GetByObjectTypeAsync(asset.Type, cancellationToken);
            var patchToApply = isFanIn
                ? MergePatchWithExistingForFanIn(
                    currentState?.Properties,
                    patch,
                    firstVisitKeysForAsset ?? new HashSet<string>(StringComparer.Ordinal))
                : patch;
            var mergedState = ComputeState(
                assetId,
                currentState,
                patchToApply,
                objectTypeSchema,
                asset,
                ComputeSimulationDeltaTime(currentState, runRecord));

            var occurredAt = DateTimeOffset.UtcNow;
            var propertyChanges = BuildPropertyChanges(currentState?.Properties, mergedState.Properties);
            var statusChanged = !string.Equals(currentState?.Status, mergedState.Status, StringComparison.OrdinalIgnoreCase);
            var hasChange = propertyChanges.Count > 0 || statusChanged;
            var serializableProps = ToSerializableProperties(mergedState.Properties);
            var nodeEvent = new EventDto
            {
                AssetId = assetId,
                EventType = EventTypes.SimulationStateUpdated,
                OccurredAt = occurredAt,
                SimulationRunId = runId,
                RunTick = runTick,
                RelationshipId = null,
                Payload = BuildStateUpdatePayload(runTick, depth, serializableProps, propertyChanges, mergedState.Status),
            };
            mergedStates[assetId] = mergedState;
            if (hasChange && !dryRun)
            {
                changedAssetIds.Add(assetId);
                await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: false, cancellationToken: cancellationToken);
            }
            else if (dryRun)
            {
                await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: true, cancellationToken: cancellationToken);
            }

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

                if (appliedRule is not null
                    && string.Equals(rel.RelationshipType, "Supplies", StringComparison.OrdinalIgnoreCase)
                    && nextPatch.Properties.Count == 0
                    && SuppliesRelationshipExpectsMappedOrLegacyTransfer(rel))
                {
                    _logger.LogWarning(
                        "Simulation Supplies empty OutgoingPatch (verify relationship FromProperty vs from-asset state/metadata). run={RunId} tick={Tick} depth={Depth} rel={RelationshipId} {FromAssetId}->{ToAssetId} mappingCount={MappingCount} fromType={FromType}",
                        runId,
                        runTick,
                        depth + 1,
                        rel.Id,
                        assetId,
                        rel.ToAssetId,
                        rel.Mappings?.Count ?? 0,
                        asset?.Type ?? "?");
                }

                if (string.Equals(rel.FromAssetId, rel.ToAssetId, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Self-loop edge during propagation: {AssetId} (run {RunId}, tick {Tick})", assetId, runId, runTick);
                    AccumulateCyclePatch(cycleAccumulatedPatches, rel.ToAssetId, nextPatch);
                }
                else if (visited.Contains(rel.ToAssetId))
                {
                    if (IsBackEdgeInBfsTree(parent, assetId, rel.ToAssetId))
                    {
                        _logger.LogWarning("Cycle back-edge during propagation: {FromAssetId} -> {ToAssetId} (run {RunId}, tick {Tick})", assetId, rel.ToAssetId, runId, runTick);
                        AccumulateCyclePatch(cycleAccumulatedPatches, rel.ToAssetId, nextPatch);
                    }
                    else
                    {
                        queue.Enqueue((rel.ToAssetId, nextPatch, depth + 1, assetId));
                    }
                }
                else
                {
                    queue.Enqueue((rel.ToAssetId, nextPatch, depth + 1, assetId));
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
            if (request.ResetState && dryRun)
                currentState = null;
            var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            var objectTypeSchema = asset is null
                ? null
                : await _objectTypeSchemaRepository.GetByObjectTypeAsync(asset.Type, cancellationToken);
            var effectivePatch = BuildEffectiveCyclePatch(currentState, patch);
            var mergedState = ComputeState(
                assetId,
                currentState,
                effectivePatch,
                objectTypeSchema,
                asset,
                ComputeSimulationDeltaTime(currentState, runRecord));

            var propertyChanges = BuildPropertyChanges(currentState?.Properties, mergedState.Properties);
            var statusChanged = !string.Equals(currentState?.Status, mergedState.Status, StringComparison.OrdinalIgnoreCase);
            var hasChange = propertyChanges.Count > 0 || statusChanged;
            var serializableProps = ToSerializableProperties(mergedState.Properties);
            var nodeEvent = new EventDto
            {
                AssetId = assetId,
                EventType = EventTypes.SimulationStateUpdated,
                OccurredAt = DateTimeOffset.UtcNow,
                SimulationRunId = runId,
                RunTick = runTick,
                RelationshipId = null,
                Payload = BuildStateUpdatePayload(runTick, maxDepth, serializableProps, propertyChanges, mergedState.Status, cycleAccumulated: true),
            };
            mergedStates[assetId] = mergedState;
            if (hasChange && !dryRun)
            {
                changedAssetIds.Add(assetId);
                await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: false, cancellationToken: cancellationToken);
            }
            else if (dryRun)
            {
                await _applier.ApplyAsync(nodeEvent, mergedState, dryRun: true, cancellationToken: cancellationToken);
            }
        }

        return new RunPropagationOutcome(mergedStates, changedAssetIds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object>> BuildBaselineInitialSnapshotAsync(
        IReadOnlyCollection<string> participatingAssetIds,
        CancellationToken cancellationToken = default)
    {
        var snapshot = new Dictionary<string, object>(StringComparer.Ordinal);
        var emptyPatch = new StatePatchDto();
        var delta = ComputeSimulationDeltaTime(null, null);

        foreach (var assetId in participatingAssetIds)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken);
            var objectTypeSchema = asset is null
                ? null
                : await _objectTypeSchemaRepository.GetByObjectTypeAsync(asset.Type, cancellationToken);
            var mergedState = ComputeState(
                assetId,
                null,
                emptyPatch,
                objectTypeSchema,
                asset,
                delta);
            var props = mergedState.Properties
                .Where(kv => kv.Value != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);
            snapshot[assetId] = new Dictionary<string, object>
            {
                ["properties"] = props,
                ["simulationStatus"] = mergedState.SimulationStatus ?? mergedState.Status,
            };
        }

        return snapshot;
    }

    private static TimeSpan ComputeSimulationDeltaTime(StateDto? current, SimulationRunDto? run)
    {
        var last = current?.UpdatedAt ?? run?.StartedAt ?? DateTimeOffset.UtcNow;
        return SimulationEngineConstants.ClampSimulationDelta(DateTimeOffset.UtcNow - last);
    }

    private static int ResolveMaxDepth(int maxDepth) =>
        maxDepth <= 0 ? SimulationEngineConstants.DefaultLeafPropagationMaxDepth : maxDepth;

    /// <summary>
    /// Fan-in (DAG convergence): merge an incoming patch for a node that was already visited this pass.
    /// Supplies edges carry <b>absolute</b> source values. Summing with persisted state for keys the first visit
    /// did not patch would add onto stale values (e.g. power-only first visit, then stream patch).
    /// Keys listed in <paramref name="firstVisitPatchedKeysForAsset"/> were present on the first inbound patch
    /// this propagation; for those, numeric values still sum (e.g. two suppliers mapping into the same target key).
    /// Other keys: incoming overwrites. Graph cycles use <see cref="AccumulateCyclePatch"/> instead.
    /// </summary>
    private static StatePatchDto MergePatchWithExistingForFanIn(
        IReadOnlyDictionary<string, object?>? currentProps,
        StatePatchDto incoming,
        IReadOnlySet<string> firstVisitPatchedKeysForAsset)
    {
        var props = new Dictionary<string, object?>();
        foreach (var kv in incoming.Properties)
        {
            if (kv.Value is null)
            {
                props[kv.Key] = null;
                continue;
            }

            if (firstVisitPatchedKeysForAsset.Contains(kv.Key)
                && currentProps != null
                && SimulationPropertyKeyNormalizer.TryGetValue(currentProps, kv.Key, out var cur)
                && cur is not null
                && TryToDouble(cur, out var curNum)
                && TryToDouble(kv.Value, out var incNum))
            {
                props[kv.Key] = curNum + incNum;
            }
            else
            {
                props[kv.Key] = kv.Value;
            }
        }

        return new StatePatchDto
        {
            Properties = props,
            Status = incoming.Status,
            LastEventType = incoming.LastEventType
        };
    }

    /// <summary>
    /// True if <paramref name="toAssetId"/> lies on the BFS tree path from <paramref name="fromAssetId"/> toward the root
    /// (i.e. it is an ancestor of <paramref name="fromAssetId"/>). Those edges are treated as cycle back-edges.
    /// </summary>
    private static bool IsBackEdgeInBfsTree(Dictionary<string, string?> parent, string fromAssetId, string toAssetId)
    {
        var cur = fromAssetId;
        while (parent.TryGetValue(cur, out var p))
        {
            if (p == toAssetId)
                return true;
            if (p == null)
                break;
            cur = p;
        }

        return false;
    }

    private StateDto ComputeState(
        string assetId,
        StateDto? current,
        StatePatchDto patch,
        ObjectTypeSchemaDto? objectTypeSchema,
        AssetDto? asset,
        TimeSpan deltaTime)
    {
        if (objectTypeSchema is null)
            return MergeStateFallback(assetId, current, patch, asset);

        var now = DateTimeOffset.UtcNow;
        var currentRaw = new Dictionary<string, object?>(current?.Properties ?? new Dictionary<string, object?>());
        var mergedProperties = new Dictionary<string, object?>(currentRaw);

        var effectiveProperties = EffectivePropertySetResolver.Resolve(objectTypeSchema, asset);
        foreach (var definition in effectiveProperties)
        {
            SimulationPropertyKeyNormalizer.TryGetValue(currentRaw, definition.Key, out var currentValue);
            SimulationPropertyKeyNormalizer.TryGetValue(patch.Properties, definition.Key, out var patchValue);
            if (definition.Mutability == Mutability.Immutable)
                patchValue = null;

            object? metadataSeed = null;
            if (definition.BaseValue is null && asset?.Metadata.Count > 0
                && SimulationPropertyKeyNormalizer.TryGetMetadataValue(asset.Metadata, definition.Key, out var metaVal)
                && metaVal is not null)
                metadataSeed = metaVal;

            var definitionForSim = metadataSeed is null
                ? definition
                : definition with { BaseValue = metadataSeed };

            if (!_simulators.TryGetValue(definitionForSim.SimulationBehavior, out var simulator))
            {
                _logger.LogWarning("No simulator registered for behavior {Behavior}; fallback to settable", definitionForSim.SimulationBehavior);
                simulator = _defaultSimulator;
            }

            var computed = simulator.Compute(new PropertySimulationContext
            {
                Definition = definitionForSim,
                CurrentValue = currentValue,
                PatchValue = patchValue,
                DeltaTime = deltaTime,
                AllProperties = mergedProperties
            });
            if (computed is null)
            {
                mergedProperties.Remove(definition.Key);
                mergedProperties.Remove(SimulationPropertyKeyNormalizer.ToPersistenceKey(definition.Key));
            }
            else
            {
                var persistAlias = SimulationPropertyKeyNormalizer.ToPersistenceKey(definition.Key);
                mergedProperties.Remove(persistAlias);
                mergedProperties[definition.Key] = computed;
            }
        }

        foreach (var kv in patch.Properties.Where(kv =>
                     effectiveProperties.All(p => !SimulationPropertyKeyNormalizer.MatchesDefinitionKey(p.Key, kv.Key))))
        {
            if (kv.Value is null) mergedProperties.Remove(kv.Key);
            else mergedProperties[kv.Key] = kv.Value;
        }

        OverlayAssetMetadataGaps(mergedProperties, asset);

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

    private static void OverlayAssetMetadataGaps(Dictionary<string, object?> mergedProperties, AssetDto? asset)
    {
        if (asset?.Metadata is not { Count: > 0 })
            return;
        foreach (var kv in asset.Metadata)
        {
            if (SimulationAssetMetadataKeys.IsReservedForPropertyOverlay(kv.Key) || kv.Value is null)
                continue;
            if (!mergedProperties.ContainsKey(kv.Key))
                mergedProperties[kv.Key] = kv.Value;
        }
    }

    private static StateDto MergeStateFallback(string assetId, StateDto? current, StatePatchDto patch, AssetDto? asset)
    {
        var now = DateTimeOffset.UtcNow;
        var mergedProperties = new Dictionary<string, object?>(current?.Properties ?? new Dictionary<string, object?>());
        OverlayAssetMetadataGaps(mergedProperties, asset);

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


    private static Dictionary<string, object> ToSerializableProperties(IReadOnlyDictionary<string, object?> props)
    {
        var d = new Dictionary<string, object>();
        foreach (var kv in props)
        {
            if (kv.Value is null) continue;
            d[kv.Key] = kv.Value;
        }
        return d;
    }

    private static Dictionary<string, object> BuildPropertyChanges(
        IReadOnlyDictionary<string, object?>? before,
        IReadOnlyDictionary<string, object?> after)
    {
        var changes = new Dictionary<string, object>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (before != null)
        {
            foreach (var k in before.Keys)
                keys.Add(k);
        }
        foreach (var k in after.Keys)
            keys.Add(k);

        foreach (var key in keys)
        {
            object? v0 = null;
            object? v1 = null;
            if (before != null)
                before.TryGetValue(key, out v0);
            after.TryGetValue(key, out v1);
            if (ValuesEqual(v0, v1))
                continue;
            changes[key] = new Dictionary<string, object?> { ["from"] = v0, ["to"] = v1 };
        }
        return changes;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
            return Math.Abs(da - db) < 1e-9;
        return Equals(a, b);
    }

    private static Dictionary<string, object> BuildStateUpdatePayload(
        int runTick,
        int depth,
        Dictionary<string, object> properties,
        Dictionary<string, object> propertyChanges,
        string simulationStatus,
        bool cycleAccumulated = false)
    {
        var payload = new Dictionary<string, object>
        {
            ["tick"] = runTick,
            ["tickIndex"] = runTick,
            ["depth"] = depth,
            ["properties"] = properties,
            ["simulationStatus"] = simulationStatus,
        };
        if (propertyChanges.Count > 0)
            payload["propertyChanges"] = propertyChanges;
        if (cycleAccumulated)
            payload["cycleAccumulated"] = true;
        return payload;
    }

    private static bool SuppliesRelationshipExpectsMappedOrLegacyTransfer(RelationshipDto rel)
    {
        if (rel.Mappings is { Count: > 0 })
            return true;
        return TransferSpecParser.Parse(rel.Properties).Count > 0;
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
