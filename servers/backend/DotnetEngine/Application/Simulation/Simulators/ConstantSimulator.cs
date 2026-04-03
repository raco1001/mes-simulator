using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation.Simulators;

public sealed class ConstantSimulator : IPropertySimulator
{
    public SimulationBehavior Behavior => SimulationBehavior.Constant;

    public object? Compute(PropertySimulationContext ctx) => ctx.CurrentValue ?? ctx.Definition.BaseValue;
}
