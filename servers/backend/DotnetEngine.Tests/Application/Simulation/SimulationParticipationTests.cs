using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class SimulationParticipationTests
{
    private static RelationshipDto Supplies(string id, string from, string to, DateTimeOffset t) => new()
    {
        Id = id,
        FromAssetId = from,
        ToAssetId = to,
        RelationshipType = "Supplies",
        Mappings = [],
        Properties = new Dictionary<string, object>(),
        CreatedAt = t,
        UpdatedAt = t,
    };

    [Fact]
    public async Task GetParticipating_IncludesSuppliesUpstream_NotReachableByForwardBfsAlone()
    {
        var t = DateTimeOffset.UtcNow;
        var rels = new[]
        {
            Supplies("r1", "seed-a", "hub", t),
            Supplies("r2", "supplier-x", "hub", t),
        };

        var mock = new Mock<IRelationshipRepository>();
        mock.Setup(r => r.GetOutgoingAsync("seed-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RelationshipDto> { rels[0] });
        mock.Setup(r => r.GetOutgoingAsync("hub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetOutgoingAsync("supplier-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetOutgoingAsync(It.Is<string>(id => id != "seed-a" && id != "hub" && id != "supplier-x"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rels);

        var result = await SimulationParticipation.GetParticipatingAssetIdsAsync(
            ["seed-a"],
            mock.Object,
            CancellationToken.None);

        Assert.Contains("seed-a", result);
        Assert.Contains("hub", result);
        Assert.Contains("supplier-x", result);
    }

    [Fact]
    public async Task GetParticipating_ChainedSuppliesUpstream_AllIncluded()
    {
        var t = DateTimeOffset.UtcNow;
        var rels = new[]
        {
            Supplies("r1", "seed", "consumer", t),
            Supplies("r2", "mid-supplier", "consumer", t),
            Supplies("r3", "root-supplier", "mid-supplier", t),
        };

        var mock = new Mock<IRelationshipRepository>();
        mock.Setup(r => r.GetOutgoingAsync("seed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RelationshipDto> { rels[0] });
        mock.Setup(r => r.GetOutgoingAsync("consumer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetOutgoingAsync("mid-supplier", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RelationshipDto> { rels[2] });
        mock.Setup(r => r.GetOutgoingAsync("root-supplier", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetOutgoingAsync(It.Is<string>(id => id != "seed" && id != "consumer" && id != "mid-supplier" && id != "root-supplier"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rels);

        var result = await SimulationParticipation.GetParticipatingAssetIdsAsync(
            ["seed"],
            mock.Object,
            CancellationToken.None);

        Assert.Contains("consumer", result);
        Assert.Contains("mid-supplier", result);
        Assert.Contains("root-supplier", result);
    }
}
