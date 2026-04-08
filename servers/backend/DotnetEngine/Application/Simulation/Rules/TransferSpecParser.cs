using System.Text.Json;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Rules;

public readonly record struct TransferSpec(string Key, double Ratio, string TargetKey);

public static class TransferSpecParser
{
    /// <summary>
    /// Source properties for Supplies propagation: merge from-asset state with incoming patch (patch overlays).
    /// If only incoming were used when non-empty, derived/computed fields on <paramref name="fromState"/> (e.g. <c>stream_out</c>)
    /// would be invisible to <see cref="PropertyMappingPropagation.ApplyMappings"/>.
    /// </summary>
    public static Dictionary<string, object?> ResolveSourceProperties(StatePatchDto incoming, StateDto? fromState)
    {
        var merged = new Dictionary<string, object?>();
        foreach (var kv in fromState?.Metadata ?? new Dictionary<string, object>())
        {
            if (SimulationAssetMetadataKeys.IsReservedForPropertyOverlay(kv.Key) || kv.Value is null)
                continue;
            merged[kv.Key] = kv.Value;
        }

        foreach (var kv in fromState?.Properties ?? new Dictionary<string, object?>())
        {
            if (kv.Value is not null)
                merged[kv.Key] = kv.Value;
        }

        foreach (var kv in incoming.Properties)
        {
            if (kv.Value is null)
                merged.Remove(kv.Key);
            else
                merged[kv.Key] = kv.Value;
        }

        return merged;
    }

    public static IReadOnlyList<TransferSpec> Parse(IReadOnlyDictionary<string, object> relationshipProperties)
    {
        if (!relationshipProperties.TryGetValue("transfers", out var raw) || raw is null)
            return [];

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return ParseFromJsonArray(je);

        if (raw is IEnumerable<object> list)
            return ParseFromObjectList(list);

        return [];
    }

    public static Dictionary<string, object?> BuildTransferredProperties(
        IReadOnlyList<TransferSpec> specs,
        StatePatchDto incoming,
        StateDto? fromState)
    {
        if (specs.Count == 0)
            return ResolveSourceProperties(incoming, fromState);

        var source = ResolveSourceProperties(incoming, fromState);
        var result = new Dictionary<string, object?>();

        foreach (var spec in specs)
        {
            if (!source.TryGetValue(spec.Key, out var value) || value is null)
                continue;

            if (TryCoerceDouble(value, out var number))
                result[spec.TargetKey] = number * spec.Ratio;
            else
                result[spec.TargetKey] = value;
        }

        return result;
    }

    private static IReadOnlyList<TransferSpec> ParseFromJsonArray(JsonElement array)
    {
        var specs = new List<TransferSpec>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("key", out var keyNode))
                continue;

            var key = keyNode.GetString();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var ratio = item.TryGetProperty("ratio", out var ratioNode) && ratioNode.TryGetDouble(out var r) ? r : 1d;
            var target = item.TryGetProperty("as", out var asNode) ? asNode.GetString() : null;
            specs.Add(new TransferSpec(key, ratio, string.IsNullOrWhiteSpace(target) ? key : target!));
        }
        return specs;
    }

    private static IReadOnlyList<TransferSpec> ParseFromObjectList(IEnumerable<object> list)
    {
        var specs = new List<TransferSpec>();
        foreach (var item in list)
        {
            if (item is not IReadOnlyDictionary<string, object> dict)
                continue;
            if (!dict.TryGetValue("key", out var keyObj))
                continue;
            var key = keyObj?.ToString();
            if (string.IsNullOrWhiteSpace(key))
                continue;
            var ratio = dict.TryGetValue("ratio", out var ratioObj) && TryCoerceDouble(ratioObj, out var r) ? r : 1d;
            var target = dict.TryGetValue("as", out var asObj) ? asObj?.ToString() : null;
            specs.Add(new TransferSpec(key!, ratio, string.IsNullOrWhiteSpace(target) ? key! : target!));
        }
        return specs;
    }

    public static bool TryCoerceDouble(object? value, out double number)
    {
        if (value is null)
        {
            number = 0;
            return false;
        }
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
                return je.TryGetDouble(out number);
            if (je.ValueKind == JsonValueKind.String && double.TryParse(je.GetString(), out var parsed))
            {
                number = parsed;
                return true;
            }
            number = 0;
            return false;
        }
        if (value is IConvertible)
        {
            try
            {
                number = Convert.ToDouble(value);
                return true;
            }
            catch
            {
            }
        }
        number = 0;
        return false;
    }
}
