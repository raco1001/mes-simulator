using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation.Simulators;

public sealed class SettableSimulator : IPropertySimulator
{
    public SimulationBehavior Behavior => SimulationBehavior.Settable;

    public object? Compute(PropertySimulationContext ctx) =>
        ctx.PatchValue ?? ctx.CurrentValue ?? ctx.Definition.BaseValue;
}
