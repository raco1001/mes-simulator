using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation.Simulators;

public sealed class DerivedSimulator : IPropertySimulator
{
    public SimulationBehavior Behavior => SimulationBehavior.Derived;

    public object? Compute(PropertySimulationContext ctx)
    {
        if (!ctx.Definition.Constraints.TryGetValue("dependsOn", out var dependsOnObj))
            return ctx.CurrentValue ?? ctx.Definition.BaseValue;

        var keys = SimulatorValueParser.ToStringList(dependsOnObj);
        if (keys.Count == 0)
            return ctx.CurrentValue ?? ctx.Definition.BaseValue;

        var values = keys
            .Where(k => ctx.AllProperties.ContainsKey(k))
            .Select(k => SimulatorValueParser.ToDouble(ctx.AllProperties[k]))
            .ToList();
        if (values.Count == 0)
            return ctx.CurrentValue ?? ctx.Definition.BaseValue;

        var op = "sum";
        if (ctx.Definition.Constraints.TryGetValue("operation", out var operationObj))
            op = (SimulatorValueParser.ToStringValue(operationObj) ?? "sum").ToLowerInvariant();

        return op switch
        {
            "avg" or "average" => values.Average(),
            "min" => values.Min(),
            "max" => values.Max(),
            _ => values.Sum(),
        };
    }
}
