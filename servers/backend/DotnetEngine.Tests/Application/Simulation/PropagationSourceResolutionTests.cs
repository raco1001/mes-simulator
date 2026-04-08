using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Rules;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class PropagationSourceResolutionTests
{
    [Fact]
    public void ResolveSourceProperties_MergesIncomingOntoFromState()
    {
        var incoming = new StatePatchDto
        {
            Properties = new Dictionary<string, object?> { ["partial"] = 1d },
        };
        var fromState = new StateDto
        {
            AssetId = "a",
            Properties = new Dictionary<string, object?>
            {
                ["streamOut"] = 3500d,
                ["other"] = 2d,
            },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var merged = TransferSpecParser.ResolveSourceProperties(incoming, fromState);

        Assert.Equal(3500d, merged["streamOut"]);
        Assert.Equal(2d, merged["other"]);
        Assert.Equal(1d, merged["partial"]);
    }

    [Fact]
    public void ResolveSourceProperties_IncomingNullRemovesKey()
    {
        var incoming = new StatePatchDto
        {
            Properties = new Dictionary<string, object?> { ["partial"] = null },
        };
        var fromState = new StateDto
        {
            AssetId = "a",
            Properties = new Dictionary<string, object?> { ["partial"] = 99d },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var merged = TransferSpecParser.ResolveSourceProperties(incoming, fromState);

        Assert.False(merged.ContainsKey("partial"));
    }

    [Fact]
    public void ResolveSourceProperties_FromStateMetadata_OverlayBeforeProperties()
    {
        var incoming = new StatePatchDto { Properties = new Dictionary<string, object?>() };
        var fromState = new StateDto
        {
            AssetId = "sup",
            Properties = new Dictionary<string, object?>(),
            Metadata = new Dictionary<string, object> { ["power"] = 5000d },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var merged = TransferSpecParser.ResolveSourceProperties(incoming, fromState);

        Assert.Equal(5000d, merged["power"]);
    }

    [Fact]
    public void ResolveSourceProperties_PropertiesWinOverMetadataSameKey()
    {
        var incoming = new StatePatchDto { Properties = new Dictionary<string, object?>() };
        var fromState = new StateDto
        {
            AssetId = "sup",
            Properties = new Dictionary<string, object?> { ["power"] = 100d },
            Metadata = new Dictionary<string, object> { ["power"] = 5000d },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var merged = TransferSpecParser.ResolveSourceProperties(incoming, fromState);

        Assert.Equal(100d, merged["power"]);
    }

    [Fact]
    public void ApplyMappings_ResolvesFromPropertyAlias_stream_out_vs_streamOut()
    {
        var source = new Dictionary<string, object?>
        {
            ["streamOut"] = 3500d,
            ["noise"] = 1d,
        };
        var mappings = new[]
        {
            new PropertyMapping("stream_out", "stream_in_1"),
        };

        var transferred = PropertyMappingPropagation.ApplyMappings(mappings, source);

        Assert.Single(transferred);
        Assert.Equal(3500d, transferred["stream_in_1"]);
    }

    [Fact]
    public void ApplyMappings_ResolvesPowerFromLowercaseMetadataStyleKey()
    {
        var source = new Dictionary<string, object?> { ["power"] = "500" };
        var mappings = new[] { new PropertyMapping("Power", "power_in", "value * 0.2", "kW", "kW") };

        var transferred = PropertyMappingPropagation.ApplyMappings(mappings, source);

        Assert.Equal(100d, transferred["power_in"]);
    }
}
