using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class PropertyMappingPropagationTests
{
    [Fact]
    public void ApplyTransform_ClampValueNM_Clips()
    {
        var v = PropertyMappingPropagation.ApplyTransform("clamp value 0 100", 150);
        Assert.Equal(100, v);
        Assert.Equal(0, PropertyMappingPropagation.ApplyTransform("clamp value 0 100", -5));
    }

    [Fact]
    public void ApplyTransform_MinMax_Abs()
    {
        Assert.Equal(3, PropertyMappingPropagation.ApplyTransform("min value 3", 10));
        Assert.Equal(10, PropertyMappingPropagation.ApplyTransform("max value 3", 10));
        Assert.Equal(5, PropertyMappingPropagation.ApplyTransform("abs value", -5));
    }

    [Fact]
    public void ApplyMappings_Converts_kW_to_W()
    {
        var source = new Dictionary<string, object?> { ["p"] = 2d };
        var mappings = new[]
        {
            new PropertyMapping("p", "load", "value", "kW", "W"),
        };
        var result = PropertyMappingPropagation.ApplyMappings(mappings, source);
        Assert.Equal(2000d, result["load"]);
    }

    [Fact]
    public void ApplyMappings_PowerFromProperty_FallsBackToPowerOutWhenPowerMissing()
    {
        var source = new Dictionary<string, object?> { ["powerOut"] = 50_000d };
        var mappings = new[]
        {
            new PropertyMapping("power", "power_in", "value * 0.02", "kW", "kW"),
        };
        var result = PropertyMappingPropagation.ApplyMappings(mappings, source);
        Assert.Equal(1000d, result["power_in"]);
    }

    [Fact]
    public void ApplyMappings_FromPropertyPowerIn_DoesNotFallbackToPowerOut()
    {
        var source = new Dictionary<string, object?> { ["powerOut"] = 5000d };
        var mappings = new[]
        {
            new PropertyMapping("power_in", "target", "value", "kW", "kW"),
        };
        var result = PropertyMappingPropagation.ApplyMappings(mappings, source);
        Assert.Empty(result);
    }

    [Fact]
    public void ApplyMappings_IncompatibleUnits_UsesValueAndStillAppliesTransform()
    {
        var source = new Dictionary<string, object?> { ["p"] = 5000d };
        var mappings = new[]
        {
            new PropertyMapping("p", "power_in", "value * 0.2", "kW", "itemsPerHour"),
        };
        var result = PropertyMappingPropagation.ApplyMappings(mappings, source);
        Assert.Equal(1000d, result["power_in"]);
    }
}
