using System.Globalization;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Rules;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Shared property mapping for Supplies, ConnectedTo, Contains when <see cref="RelationshipDto.Mappings"/> is non-empty.
/// </summary>
public static class PropertyMappingPropagation
{
    public static Dictionary<string, object?> ApplyMappings(
        IReadOnlyList<PropertyMapping> mappings,
        IReadOnlyDictionary<string, object?> source)
    {
        var result = new Dictionary<string, object?>();
        foreach (var m in mappings)
        {
            if (string.IsNullOrWhiteSpace(m.FromProperty) || string.IsNullOrWhiteSpace(m.ToProperty))
                continue;
            object? raw = null;
            double value;
            if (SimulationPropertyKeyNormalizer.TryGetValue(source, m.FromProperty, out raw)
                && raw is not null
                && TransferSpecParser.TryCoerceDouble(raw, out value))
            {
                // use primary FromProperty
            }
            else if (TryResolvePowerSourceFallback(source, m.FromProperty, out raw, out value))
            {
                // e.g. map "Power" / power but only powerOut is populated on supplier state
            }
            else
                continue;

            // Lenient: if both ends specify units we cannot convert (e.g. kW vs catalog label), still apply the transform.
            if (!SimpleUnitConverter.TryConvert(value, m.FromUnit, m.ToUnit, out var converted))
                converted = value;

            var final = ApplyTransform(m.TransformRule, converted);
            result[m.ToProperty] = final;
        }

        return result;
    }

    /// <summary>
    /// Supported forms:
    /// <list type="bullet">
    /// <item><c>value</c> — identity</item>
    /// <item><c>value * N</c>, <c>value / N</c>, <c>value + N</c>, <c>value - N</c></item>
    /// <item><c>min value N</c>, <c>max value N</c></item>
    /// <item><c>abs value</c></item>
    /// <item><c>clamp value N M</c> — inclusive [N, M] (N and M are numbers)</item>
    /// </list>
    /// </summary>
    public static double ApplyTransform(string rule, double value)
    {
        var trimmed = rule.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("value", StringComparison.OrdinalIgnoreCase))
            return value;

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return value;

        // clamp value N M
        if (parts.Length == 4
            && parts[0].Equals("clamp", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("value", StringComparison.OrdinalIgnoreCase)
            && TryParseDouble(parts[2], out var lo)
            && TryParseDouble(parts[3], out var hi))
        {
            var min = Math.Min(lo, hi);
            var max = Math.Max(lo, hi);
            return Math.Clamp(value, min, max);
        }

        // min value N / max value N
        if (parts.Length == 3
            && parts[1].Equals("value", StringComparison.OrdinalIgnoreCase)
            && TryParseDouble(parts[2], out var bound))
        {
            if (parts[0].Equals("min", StringComparison.OrdinalIgnoreCase))
                return Math.Min(value, bound);
            if (parts[0].Equals("max", StringComparison.OrdinalIgnoreCase))
                return Math.Max(value, bound);
        }

        // abs value
        if (parts.Length == 2
            && parts[0].Equals("abs", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Abs(value);
        }

        // value op N
        if (parts.Length == 3
            && parts[0].Equals("value", StringComparison.OrdinalIgnoreCase)
            && TryParseDouble(parts[2], out var operand))
        {
            return parts[1] switch
            {
                "*" => value * operand,
                "/" => operand != 0 ? value / operand : value,
                "+" => value + operand,
                "-" => value - operand,
                _ => value
            };
        }

        return value;
    }

    private static bool TryParseDouble(string s, out double d) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d);

    private static string NormalizeMappingFromKey(string key) =>
        key.Trim().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    /// <summary>
    /// When FromProperty is the canonical "power" slot but state holds the output on <c>powerOut</c> (common for Power Supplyer UIs).
    /// </summary>
    private static bool TryResolvePowerSourceFallback(
        IReadOnlyDictionary<string, object?> source,
        string fromProperty,
        out object? raw,
        out double value)
    {
        raw = null;
        value = 0;
        var n = NormalizeMappingFromKey(fromProperty);
        // Explicit powerOut mapping should use primary path, not this fallback.
        if (n != "power")
            return false;

        foreach (var alt in new[] { "powerOut", "powerOutput", "power_out" })
        {
            if (SimulationPropertyKeyNormalizer.TryGetValue(source, alt, out var r)
                && r is not null
                && TransferSpecParser.TryCoerceDouble(r, out value))
            {
                raw = r;
                return true;
            }
        }

        return false;
    }
}
