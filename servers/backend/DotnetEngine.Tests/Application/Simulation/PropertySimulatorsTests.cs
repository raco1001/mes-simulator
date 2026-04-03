using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Simulators;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class PropertySimulatorsTests
{
    [Fact]
    public void ConstantSimulator_IgnoresPatch()
    {
        var sut = new ConstantSimulator();
        var result = sut.Compute(new PropertySimulationContext
        {
            Definition = MakeDefinition(SimulationBehavior.Constant),
            CurrentValue = 10d,
            PatchValue = 99d,
            DeltaTime = TimeSpan.FromSeconds(1)
        });
        Assert.Equal(10d, result);
    }

    [Fact]
    public void SettableSimulator_UsesPatchWhenPresent()
    {
        var sut = new SettableSimulator();
        var result = sut.Compute(new PropertySimulationContext
        {
            Definition = MakeDefinition(SimulationBehavior.Settable),
            CurrentValue = 10d,
            PatchValue = 11d,
            DeltaTime = TimeSpan.FromSeconds(1)
        });
        Assert.Equal(11d, result);
    }

    [Fact]
    public void RateSimulator_AppliesDeltaTime()
    {
        var sut = new RateSimulator();
        var result = sut.Compute(new PropertySimulationContext
        {
            Definition = MakeDefinition(SimulationBehavior.Rate, baseValue: 0d),
            CurrentValue = 10d,
            PatchValue = 2d,
            DeltaTime = TimeSpan.FromSeconds(3)
        });
        Assert.Equal(16d, result);
    }

    [Fact]
    public void AccumulatorSimulator_ClampsByMinMax()
    {
        var sut = new AccumulatorSimulator();
        var result = sut.Compute(new PropertySimulationContext
        {
            Definition = MakeDefinition(
                SimulationBehavior.Accumulator,
                constraints: new Dictionary<string, object?> { ["min"] = 0d, ["max"] = 100d }),
            CurrentValue = 95d,
            PatchValue = 20d,
            DeltaTime = TimeSpan.FromSeconds(1)
        });
        Assert.Equal(100d, result);
    }

    [Fact]
    public void DerivedSimulator_ComputesFromDependsOn()
    {
        var sut = new DerivedSimulator();
        var result = sut.Compute(new PropertySimulationContext
        {
            Definition = MakeDefinition(
                SimulationBehavior.Derived,
                constraints: new Dictionary<string, object?>
                {
                    ["dependsOn"] = new List<object?> { "a", "b" },
                    ["operation"] = "sum"
                }),
            CurrentValue = 0d,
            PatchValue = null,
            DeltaTime = TimeSpan.FromSeconds(1),
            AllProperties = new Dictionary<string, object?> { ["a"] = 10d, ["b"] = 5d }
        });
        Assert.Equal(15d, result);
    }

    private static PropertyDefinition MakeDefinition(
        SimulationBehavior behavior,
        object? baseValue = null,
        IReadOnlyDictionary<string, object?>? constraints = null) =>
        new()
        {
            Key = "x",
            DataType = DataType.Number,
            SimulationBehavior = behavior,
            Mutability = Mutability.Mutable,
            BaseValue = baseValue,
            Required = true,
            Constraints = constraints ?? new Dictionary<string, object?>()
        };
}
