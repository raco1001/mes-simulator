using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Merges ObjectType schema properties with asset.metadata.extraProperties for simulation and initial state.
/// Order: schema first, then extras — duplicate keys: last definition wins in ComputeState loop.
/// </summary>
public static class EffectivePropertySetResolver
{
    public const string ExtraPropertiesMetadataKey = "extraProperties";

    public static IReadOnlyList<PropertyDefinition> Resolve(
        ObjectTypeSchemaDto? schema,
        AssetDto? asset)
    {
        var schemaProps = (IEnumerable<PropertyDefinition>)(schema?.ResolvedProperties
            ?? schema?.OwnProperties
            ?? []);

        var list = schemaProps.ToList();
        list.AddRange(ParseExtraProperties(asset?.Metadata));
        return list;
    }

    public static List<PropertyDefinition> ParseExtraProperties(
        IReadOnlyDictionary<string, object>? metadata)
    {
        var result = new List<PropertyDefinition>();
        if (metadata is null || !metadata.TryGetValue(ExtraPropertiesMetadataKey, out var raw) || raw is null)
            return result;

        switch (raw)
        {
            case List<object> listObj:
                foreach (var item in listObj)
                    TryAddParsedItem(item, result);
                break;
            case object[] arr:
                foreach (var item in arr)
                    TryAddParsedItem(item, result);
                break;
            case IList ilist when raw is not string:
                foreach (var item in ilist)
                    TryAddParsedItem(item, result);
                break;
            case JsonElement je when je.ValueKind == JsonValueKind.Array:
                foreach (var item in je.EnumerateArray())
                    TryAddParsedItem(item, result);
                break;
        }

        return result;
    }

    private static void TryAddParsedItem(object? item, List<PropertyDefinition> result)
    {
        switch (item)
        {
            case JsonElement je when je.ValueKind == JsonValueKind.Object:
                AddFromJsonElement(je, result);
                break;
            case Dictionary<string, object> dict:
                AddFromDictionary(dict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value), result);
                break;
            case IReadOnlyDictionary<string, object> ro:
                AddFromDictionary(ro.ToDictionary(kv => kv.Key, kv => (object?)kv.Value), result);
                break;
        }
    }

    private static void AddFromJsonElement(JsonElement je, List<PropertyDefinition> result)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in je.EnumerateObject())
            dict[prop.Name] = JsonElementToObject(prop.Value);
        AddFromDictionary(dict, result);
    }

    private static object? JsonElementToObject(JsonElement je) => je.ValueKind switch
    {
        JsonValueKind.String => je.GetString(),
        JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => je,
        JsonValueKind.Array => je,
        _ => null,
    };

    private static void AddFromDictionary(Dictionary<string, object?> dict, List<PropertyDefinition> result)
    {
        if (!TryGetString(dict, "key", out var key) || string.IsNullOrWhiteSpace(key))
            return;

        var dataTypeStr = TryGetString(dict, "dataType", out var ds) ? ds : "String";
        if (!Enum.TryParse<DataType>(dataTypeStr, ignoreCase: true, out var dataType))
            dataType = DataType.String;

        var simStr = TryGetString(dict, "simulationBehavior", out var sb) ? sb : "Settable";
        if (!Enum.TryParse<SimulationBehavior>(simStr, ignoreCase: true, out var sim))
            sim = SimulationBehavior.Settable;

        var mutStr = TryGetString(dict, "mutability", out var m) ? m : "Mutable";
        if (!Enum.TryParse<Mutability>(mutStr, ignoreCase: true, out var mut))
            mut = Mutability.Mutable;

        dict.TryGetValue("unit", out var unitObj);
        dict.TryGetValue("value", out var baseValue);
        baseValue = NormalizeScalarValue(baseValue);

        var constraints = ParseConstraints(dict.TryGetValue("constraints", out var co) ? co : null);

        result.Add(new PropertyDefinition
        {
            Key = key,
            DataType = dataType,
            Unit = CoerceToString(unitObj) ?? unitObj?.ToString(),
            SimulationBehavior = sim,
            Mutability = mut,
            BaseValue = baseValue,
            Constraints = constraints,
            Required = false,
        });
    }

    private static bool TryGetString(Dictionary<string, object?> dict, string key, out string value)
    {
        value = "";
        if (!dict.TryGetValue(key, out var o) || o is null)
            return false;
        var s = CoerceToString(o);
        if (string.IsNullOrWhiteSpace(s))
            return false;
        value = s;
        return true;
    }

    private static string? CoerceToString(object? o) => o switch
    {
        null => null,
        string s => s,
        JsonElement je => je.ValueKind switch
        {
            JsonValueKind.String => je.GetString(),
            JsonValueKind.Number => je.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        },
        _ => o.ToString(),
    };

    private static object? NormalizeScalarValue(object? o) => o switch
    {
        JsonElement je => JsonElementToObject(je),
        _ => o,
    };

    private static IReadOnlyDictionary<string, object?> ParseConstraints(object? co)
    {
        if (co is Dictionary<string, object> cDict)
            return cDict.ToDictionary(kv => kv.Key, kv => (object?)NormalizeScalarValue(kv.Value));

        if (co is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            var d = new Dictionary<string, object?>();
            foreach (var prop in je.EnumerateObject())
                d[prop.Name] = JsonElementToObject(prop.Value);
            return d;
        }

        return new Dictionary<string, object?>();
    }
}
