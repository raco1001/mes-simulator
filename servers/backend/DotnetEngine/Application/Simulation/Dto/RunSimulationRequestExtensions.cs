namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// Normalizes <see cref="RunSimulationRequest"/> trigger fields: non-empty <c>triggerAssetIds</c> wins over legacy <c>triggerAssetId</c>.
/// </summary>
public static class RunSimulationRequestExtensions
{
    /// <summary>
    /// Returns deduped, trimmed seed ids. Empty if neither field yields a seed.
    /// </summary>
    public static IReadOnlyList<string> ResolveTriggerAssetIds(this RunSimulationRequest request)
    {
        if (request.TriggerAssetIds is { Count: > 0 })
        {
            var list = request.TriggerAssetIds
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (list.Count > 0)
                return list.ConvertAll(s => s!);
        }

        if (!string.IsNullOrWhiteSpace(request.TriggerAssetId))
            return new[] { request.TriggerAssetId.Trim() };

        return Array.Empty<string>();
    }

    /// <summary>
    /// True if patch carries any non-default field (multi-seed MVP disallows this).
    /// </summary>
    public static bool HasMeaningfulPatch(StatePatchDto? patch)
    {
        if (patch is null)
            return false;
        if (!string.IsNullOrWhiteSpace(patch.Status) || !string.IsNullOrWhiteSpace(patch.LastEventType))
            return true;
        return patch.Properties.Count > 0 &&
               patch.Properties.Any(kv => kv.Value is not null);
    }

    /// <summary>
    /// Phase 23: more than one seed requires a null/empty effective patch.
    /// </summary>
    public static bool IsMultiSeedPatchDisallowed(IReadOnlyList<string> ids, StatePatchDto? patch) =>
        ids.Count > 1 && HasMeaningfulPatch(patch);
}
