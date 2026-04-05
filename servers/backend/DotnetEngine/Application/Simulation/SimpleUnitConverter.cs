namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Minimal unit conversion for relationship property mapping (Power, Energy, length, temperature).
/// Extend the table as product units grow.
/// </summary>
public static class SimpleUnitConverter
{
    /// <summary>
    /// Converts <paramref name="value"/> expressed in <paramref name="fromUnit"/> to <paramref name="toUnit"/>.
    /// If either unit is null/whitespace, returns true and leaves value unchanged (dimensionless / unspecified).
    /// </summary>
    public static bool TryConvert(double value, string? fromUnit, string? toUnit, out double result)
    {
        result = value;
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            return true;

        var f = fromUnit.Trim();
        var t = toUnit.Trim();
        if (string.Equals(f, t, StringComparison.OrdinalIgnoreCase))
            return true;

        // Temperature: degC ↔ degF (offset, not multiplicative only)
        if (IsTemperaturePair(f, t, out var degCtoF))
        {
            result = degCtoF ? value * 9.0 / 5.0 + 32.0 : (value - 32.0) * 5.0 / 9.0;
            return true;
        }

        if (TryMultiplicativeFactor(f, t, out var factor))
        {
            result = value * factor;
            return true;
        }

        return false;
    }

    /// <summary>
    /// True if units are the same, unspecified, or a known conversion exists.
    /// </summary>
    public static bool AreCompatible(string? fromUnit, string? toUnit)
    {
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            return true;
        if (string.Equals(fromUnit.Trim(), toUnit.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;
        return TryConvert(1.0, fromUnit, toUnit, out _);
    }

    private static bool IsTemperaturePair(string f, string t, out bool cToF)
    {
        cToF = false;
        var fc = f.ToLowerInvariant();
        var tc = t.ToLowerInvariant();
        if ((fc is "degc" or "celsius" or "°c") && (tc is "degf" or "fahrenheit" or "°f"))
        {
            cToF = true;
            return true;
        }
        if ((fc is "degf" or "fahrenheit" or "°f") && (tc is "degc" or "celsius" or "°c"))
        {
            cToF = false;
            return true;
        }
        return false;
    }

    private static bool TryMultiplicativeFactor(string from, string to, out double factor)
    {
        factor = 1;
        var a = from.ToLowerInvariant();
        var b = to.ToLowerInvariant();

        // key: (from, to) → multiply value by factor to get "to"
        (string, string)[] keys =
        [
            ("kw", "w"), ("kwh", "wh"), ("km", "m"),
        ];
        foreach (var (x, y) in keys)
        {
            if (a == x && b == y)
            {
                factor = 1000;
                return true;
            }
            if (a == y && b == x)
            {
                factor = 0.001;
                return true;
            }
        }

        return false;
    }
}
