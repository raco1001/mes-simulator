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
}
