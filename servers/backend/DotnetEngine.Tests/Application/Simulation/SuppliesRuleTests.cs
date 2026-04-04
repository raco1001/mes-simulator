using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Rules;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class SuppliesRuleTests
{
    [Fact]
    public void Apply_UsesIncomingProperties_WhenProvided()
    {
        var rule = new SuppliesRule();
        var result = rule.Apply(new PropagationContext
        {
            FromAssetId = "a",
            ToAssetId = "b",
            SimulationRunId = "run",
            RunTick = 1,
            IncomingPatch = new StatePatchDto
            {
                Properties = new Dictionary<string, object?> { ["power"] = 100 }
            },
            Relationship = new RelationshipDto
            {
                Id = "r",
                FromAssetId = "a",
                ToAssetId = "b",
                RelationshipType = "Supplies",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        });

        Assert.Equal(100, result.OutgoingPatch.Properties["power"]);
    }

    [Fact]
    public void Apply_UsesMappingsWithTransform_WhenMappingsNonEmpty()
    {
        var rule = new SuppliesRule();
        var result = rule.Apply(new PropagationContext
        {
            FromAssetId = "a",
            ToAssetId = "b",
            SimulationRunId = "run",
            RunTick = 1,
            IncomingPatch = new StatePatchDto
            {
                Properties = new Dictionary<string, object?> { ["power"] = 10d }
            },
            Relationship = new RelationshipDto
            {
                Id = "r",
                FromAssetId = "a",
                ToAssetId = "b",
                RelationshipType = "Supplies",
                Mappings =
                [
                    new PropertyMapping("power", "load", "value * 2")
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        });

        Assert.Equal(20d, result.OutgoingPatch.Properties["load"]);
        Assert.False(result.OutgoingPatch.Properties.ContainsKey("power"));
    }

    [Fact]
    public void Apply_UsesLegacyTransfers_WhenMappingsEmpty()
    {
        var rule = new SuppliesRule();
        var result = rule.Apply(new PropagationContext
        {
            FromAssetId = "a",
            ToAssetId = "b",
            SimulationRunId = "run",
            RunTick = 1,
            IncomingPatch = new StatePatchDto
            {
                Properties = new Dictionary<string, object?> { ["power"] = 50d }
            },
            Relationship = new RelationshipDto
            {
                Id = "r",
                FromAssetId = "a",
                ToAssetId = "b",
                RelationshipType = "Supplies",
                Properties = new Dictionary<string, object>
                {
                    ["transfers"] = new List<object>
                    {
                        new Dictionary<string, object> { ["key"] = "power", ["ratio"] = 2d, ["as"] = "outPower" }
                    }
                },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        });

        Assert.Equal(100d, result.OutgoingPatch.Properties["outPower"]);
    }
}
