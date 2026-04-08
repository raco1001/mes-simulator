using System;
using System.Collections.Generic;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Resolves logical property keys (schema / mapping / extraProperties, e.g. <c>stream_in_1</c>)
/// against persisted state dictionaries (Mongo round-trip uses <see cref="ToPersistenceKey"/>).
/// <para>
/// Must stay aligned with <c>MetadataBsonConverter.ToCamelCaseKey</c> in Infrastructure.
/// </para>
/// </summary>
public static class SimulationPropertyKeyNormalizer
{
    public static string ToPersistenceKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        var parts = key.Split('_');
        if (parts.Length == 1)
        {
            var p = parts[0];
            if (string.IsNullOrEmpty(p)) return key;
            return char.ToLowerInvariant(p[0]) + p[1..];
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (string.IsNullOrEmpty(p)) continue;
            parts[i] = i == 0
                ? char.ToLowerInvariant(p[0]) + p[1..].ToLowerInvariant()
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant();
        }

        return string.Concat(parts);
    }

    /// <summary>True if <paramref name="patchKey"/> is the same logical key as a schema property key.</summary>
    public static bool MatchesDefinitionKey(string definitionKey, string patchKey) =>
        string.Equals(definitionKey, patchKey, StringComparison.Ordinal)
        || string.Equals(ToPersistenceKey(definitionKey), patchKey, StringComparison.Ordinal);

    public static bool TryGetValue(
        IReadOnlyDictionary<string, object?> props,
        string logicalKey,
        out object? value)
    {
        if (props.TryGetValue(logicalKey, out value))
            return true;
        var alt = ToPersistenceKey(logicalKey);
        if (!string.Equals(alt, logicalKey, StringComparison.Ordinal) && props.TryGetValue(alt, out value))
            return true;
        return false;
    }

    /// <summary>Flat asset metadata (<c>object</c> values); same alias rules as state dictionaries.</summary>
    public static bool TryGetMetadataValue(
        IReadOnlyDictionary<string, object> props,
        string logicalKey,
        out object? value)
    {
        if (props.TryGetValue(logicalKey, out var v))
        {
            value = v;
            return true;
        }

        var alt = ToPersistenceKey(logicalKey);
        if (!string.Equals(alt, logicalKey, StringComparison.Ordinal) && props.TryGetValue(alt, out v))
        {
            value = v;
            return true;
        }

        value = null;
        return false;
    }
}
