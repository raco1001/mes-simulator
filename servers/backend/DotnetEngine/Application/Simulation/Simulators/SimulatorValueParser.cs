using System.Text.Json;

namespace DotnetEngine.Application.Simulation.Simulators;

internal static class SimulatorValueParser
{
    public static double ToDouble(object? value, double defaultValue = 0)
    {
        if (value is null)
            return defaultValue;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var d))
                return d;
            if (je.ValueKind == JsonValueKind.String && double.TryParse(je.GetString(), out var sd))
                return sd;
            return defaultValue;
        }

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public static string? ToStringValue(object? value)
    {
        if (value is null)
            return null;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString();
        return value.ToString();
    }

    public static IReadOnlyList<string> ToStringList(object? value)
    {
        if (value is null)
            return [];
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList();
        }
        if (value is IEnumerable<object?> list)
        {
            return list.Select(ToStringValue).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
        }
        return [];
    }
}
