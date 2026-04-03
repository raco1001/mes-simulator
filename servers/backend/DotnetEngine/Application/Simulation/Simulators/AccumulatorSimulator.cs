using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation.Simulators;

public sealed class AccumulatorSimulator : IPropertySimulator
{
    public SimulationBehavior Behavior => SimulationBehavior.Accumulator;

    public object? Compute(PropertySimulationContext ctx)
    {
        var stored = SimulatorValueParser.ToDouble(ctx.CurrentValue ?? ctx.Definition.BaseValue);
        var flow = SimulatorValueParser.ToDouble(ctx.PatchValue);
        var result = stored + flow;

        if (ctx.Definition.Constraints.TryGetValue("min", out var minObj))
            result = Math.Max(result, SimulatorValueParser.ToDouble(minObj, result));
        if (ctx.Definition.Constraints.TryGetValue("max", out var maxObj))
            result = Math.Min(result, SimulatorValueParser.ToDouble(maxObj, result));

        return result;
    }
}
