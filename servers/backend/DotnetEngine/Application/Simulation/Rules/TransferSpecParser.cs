using System.Text.Json;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Rules;

internal readonly record struct TransferSpec(string Key, double Ratio, string TargetKey);

internal static class TransferSpecParser
{
    /// <summary>
    /// Source properties for Supplies propagation: incoming patch wins, else from-asset state.
    /// </summary>
    public static Dictionary<string, object?> ResolveSourceProperties(StatePatchDto incoming, StateDto? fromState) =>
        incoming.Properties.Count > 0
            ? new Dictionary<string, object?>(incoming.Properties)
            : new Dictionary<string, object?>(fromState?.Properties ?? new Dictionary<string, object?>());

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

    internal static bool TryCoerceDouble(object? value, out double number)
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
