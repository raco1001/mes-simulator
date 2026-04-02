using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.Simulation;

public interface IPropertySimulator
{
    SimulationBehavior Behavior { get; }
    object? Compute(PropertySimulationContext ctx);
}
