using System;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Asset.metadata keys that must not be copied into simulation state as scalar properties.
/// Client tick payloads also strip the same UI-only keys (see <see cref="ShouldExcludeFromClientTickPayload"/>).
/// </summary>
public static class SimulationAssetMetadataKeys
{
    private static string NormalizeKey(string key) =>
        key.Trim().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    public static bool IsReservedForPropertyOverlay(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return true;
        var n = NormalizeKey(key);
        if (n == NormalizeKey(EffectivePropertySetResolver.ExtraPropertiesMetadataKey))
            return true;
        if (n == "tickintervalms") return true;
        if (n == "canvasposition") return true;
        return false;
    }

    /// <summary>
    /// Keys removed from SSE <see cref="Ports.Driven.SimulationTickEvent"/> property maps (well-known UI / layout).
    /// </summary>
    public static bool ShouldExcludeFromClientTickPayload(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return true;
        var n = NormalizeKey(key);
        if (IsReservedForPropertyOverlay(key))
            return true;
        // Title uses metadata assetName; avoid duplicate lines on trigger nodes during sim.
        if (n == "assetname") return true;
        return false;
    }
}
