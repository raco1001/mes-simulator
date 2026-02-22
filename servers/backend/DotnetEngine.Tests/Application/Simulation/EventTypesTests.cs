using DotnetEngine.Domain.Simulation.Constants;
using DotnetEngine.Domain.Simulation.ValueObjects;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class EventTypesTests
{
    [Fact]
    public void GetKind_SimulationStateUpdated_ReturnsObservation()
    {
        Assert.Equal(EventKind.Observation, EventTypes.GetKind(EventTypes.SimulationStateUpdated));
    }

    [Fact]
    public void GetKind_PowerChanged_ReturnsObservation()
    {
        Assert.Equal(EventKind.Observation, EventTypes.GetKind(EventTypes.PowerChanged));
    }

    [Fact]
    public void GetKind_StateTransitioned_ReturnsObservation()
    {
        Assert.Equal(EventKind.Observation, EventTypes.GetKind(EventTypes.StateTransitioned));
    }

    [Fact]
    public void GetKind_StartMachine_ReturnsCommand()
    {
        Assert.Equal(EventKind.Command, EventTypes.GetKind(EventTypes.StartMachine));
    }

    [Fact]
    public void GetKind_StopMachine_ReturnsCommand()
    {
        Assert.Equal(EventKind.Command, EventTypes.GetKind(EventTypes.StopMachine));
    }

    [Fact]
    public void GetKind_ChangeSpeed_ReturnsCommand()
    {
        Assert.Equal(EventKind.Command, EventTypes.GetKind(EventTypes.ChangeSpeed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown.type")]
    public void GetKind_UnknownOrEmpty_ReturnsObservation(string? eventType)
    {
        Assert.Equal(EventKind.Observation, EventTypes.GetKind(eventType));
    }
}
