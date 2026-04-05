using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Domain.Simulation;

namespace DotnetEngine.Application.Simulation.Handlers;

public sealed class WhatIfSimulationQueryHandler : IWhatIfSimulationQuery
{
    private readonly IRunSimulationCommand _runSimulationCommand;
    private readonly IAssetRepository _assetRepository;
    private readonly IRelationshipRepository _relationshipRepository;

    public WhatIfSimulationQueryHandler(
        IRunSimulationCommand runSimulationCommand,
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository)
    {
        _runSimulationCommand = runSimulationCommand;
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
    }

    public async Task<WhatIfResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var maxDepth = request.MaxDepth <= 0
            ? SimulationEngineConstants.DefaultLeafPropagationMaxDepth
            : request.MaxDepth;
        var (affected, depthMap) = await CollectAffectedAsync(request.TriggerAssetId, maxDepth, cancellationToken);
        var before = await BuildBeforeSnapshotsAsync(affected, cancellationToken);

        var runId = $"whatif-{Guid.NewGuid():N}";
        var simulated = await _runSimulationCommand.RunOnePropagationAsync(
            runId,
            request with { MaxDepth = maxDepth },
            dryRun: true,
            cancellationToken: cancellationToken);

        var after = BuildAfterSnapshots(affected, before, simulated);
        var deltas = BuildDeltas(before, after);

        return new WhatIfResult
        {
            RunId = runId,
            Before = before,
            After = after,
            Deltas = deltas,
            AffectedObjects = affected,
            PropagationDepth = depthMap.Count == 0 ? 0 : depthMap.Values.Max(),
        };
    }

    private async Task<(IReadOnlyList<string> Affected, IReadOnlyDictionary<string, int> DepthMap)> CollectAffectedAsync(
        string triggerAssetId,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        var queue = new Queue<(string AssetId, int Depth)>();
        var depthMap = new Dictionary<string, int>(StringComparer.Ordinal);
        queue.Enqueue((triggerAssetId, 0));

        while (queue.Count > 0)
        {
            var (assetId, depth) = queue.Dequeue();
            if (depth > maxDepth)
                continue;
            if (depthMap.TryGetValue(assetId, out var existingDepth) && existingDepth <= depth)
                continue;

            depthMap[assetId] = depth;

            var outgoing = await _relationshipRepository.GetOutgoingAsync(assetId, cancellationToken);
            foreach (var rel in outgoing)
                queue.Enqueue((rel.ToAssetId, depth + 1));
        }

        return ([.. depthMap.OrderBy(kv => kv.Value).Select(kv => kv.Key)], depthMap);
    }

    private async Task<IReadOnlyDictionary<string, StateSnapshot>> BuildBeforeSnapshotsAsync(
        IReadOnlyList<string> affected,
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<string, StateSnapshot>(StringComparer.Ordinal);
        foreach (var assetId in affected)
        {
            var state = await _assetRepository.GetStateByAssetIdAsync(assetId, cancellationToken);
            snapshots[assetId] = new StateSnapshot
            {
                Properties = state?.Properties ?? new Dictionary<string, object?>()
            };
        }
        return snapshots;
    }

    private static IReadOnlyDictionary<string, StateSnapshot> BuildAfterSnapshots(
        IReadOnlyList<string> affected,
        IReadOnlyDictionary<string, StateSnapshot> before,
        IReadOnlyDictionary<string, StateDto> simulated)
    {
        var snapshots = new Dictionary<string, StateSnapshot>(StringComparer.Ordinal);
        foreach (var assetId in affected)
        {
            if (simulated.TryGetValue(assetId, out var state))
            {
                snapshots[assetId] = new StateSnapshot { Properties = state.Properties };
                continue;
            }

            snapshots[assetId] = before.TryGetValue(assetId, out var prior)
                ? prior
                : new StateSnapshot { Properties = new Dictionary<string, object?>() };
        }
        return snapshots;
    }

    private static IReadOnlyList<ObjectDelta> BuildDeltas(
        IReadOnlyDictionary<string, StateSnapshot> before,
        IReadOnlyDictionary<string, StateSnapshot> after)
    {
        var objectDeltas = new List<ObjectDelta>();
        foreach (var objectId in after.Keys)
        {
            var beforeProps = before.TryGetValue(objectId, out var b) ? b.Properties : new Dictionary<string, object?>();
            var afterProps = after[objectId].Properties;
            var allKeys = beforeProps.Keys.Union(afterProps.Keys, StringComparer.Ordinal);
            var changes = new List<PropertyChange>();

            foreach (var key in allKeys)
            {
                beforeProps.TryGetValue(key, out var beforeValue);
                afterProps.TryGetValue(key, out var afterValue);
                if (Equals(beforeValue, afterValue))
                    continue;

                changes.Add(new PropertyChange
                {
                    Key = key,
                    Before = beforeValue,
                    After = afterValue,
                    Delta = BuildDelta(beforeValue, afterValue)
                });
            }

            if (changes.Count > 0)
                objectDeltas.Add(new ObjectDelta { ObjectId = objectId, Changes = changes });
        }
        return objectDeltas;
    }

    private static object? BuildDelta(object? before, object? after)
    {
        if (!TryToDouble(before, out var b) || !TryToDouble(after, out var a))
            return null;
        return a - b;
    }

    private static bool TryToDouble(object? value, out double number)
    {
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
}
