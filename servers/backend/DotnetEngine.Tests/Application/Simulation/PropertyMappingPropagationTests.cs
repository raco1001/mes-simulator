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
}
