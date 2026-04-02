using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation.Simulators;

public sealed class RateSimulator : IPropertySimulator
{
    public SimulationBehavior Behavior => SimulationBehavior.Rate;

    public object? Compute(PropertySimulationContext ctx)
    {
        var current = SimulatorValueParser.ToDouble(ctx.CurrentValue ?? ctx.Definition.BaseValue);
        var delta = SimulatorValueParser.ToDouble(ctx.PatchValue ?? ctx.Definition.BaseValue);
        return current + (delta * ctx.DeltaTime.TotalSeconds);
    }
}
