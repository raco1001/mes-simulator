using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driving;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class WhatIfSimulationQueryHandlerTests
{
    [Fact]
    public async Task RunAsync_ReturnsBefore_After_Deltas_WithDryRun()
    {
        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetStateByAssetIdAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateDto
            {
                AssetId = "a1",
                Properties = new Dictionary<string, object?> { ["temp"] = 10d },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.GetOutgoingAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var simulated = new Dictionary<string, StateDto>
        {
            ["a1"] = new StateDto
            {
                AssetId = "a1",
                Properties = new Dictionary<string, object?> { ["temp"] = 20d },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var runCommand = new Mock<IRunSimulationCommand>();
        runCommand
            .Setup(c => c.RunOnePropagationAsync(
                It.Is<string>(s => s.StartsWith("whatif-")),
                It.IsAny<RunSimulationRequest>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunPropagationOutcome(simulated, Array.Empty<string>()));

        var handler = new WhatIfSimulationQueryHandler(runCommand.Object, assetRepo.Object, relRepo.Object);

        var result = await handler.RunAsync(new RunSimulationRequest
        {
            TriggerAssetId = "a1",
            Patch = new StatePatchDto { Properties = new Dictionary<string, object?> { ["temp"] = 20d } },
            MaxDepth = 3
        });

        Assert.StartsWith("whatif-", result.RunId);
        Assert.Contains("a1", result.AffectedObjects);
        Assert.Single(result.Deltas);
        Assert.Equal("a1", result.Deltas[0].ObjectId);
        Assert.Single(result.Deltas[0].Changes);
        Assert.Equal("temp", result.Deltas[0].Changes[0].Key);
        Assert.Equal(10d, result.Deltas[0].Changes[0].Before);
        Assert.Equal(20d, result.Deltas[0].Changes[0].After);
        Assert.Equal(10d, result.Deltas[0].Changes[0].Delta);

        runCommand.Verify(c => c.RunOnePropagationAsync(
            It.IsAny<string>(),
            It.IsAny<RunSimulationRequest>(),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_NoDelta_WhenNoPropertyChange()
    {
        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetStateByAssetIdAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateDto
            {
                AssetId = "a1",
                Properties = new Dictionary<string, object?> { ["temp"] = 10d },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.GetOutgoingAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var simulated = new Dictionary<string, StateDto>
        {
            ["a1"] = new StateDto
            {
                AssetId = "a1",
                Properties = new Dictionary<string, object?> { ["temp"] = 10d },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var runCommand = new Mock<IRunSimulationCommand>();
        runCommand
            .Setup(c => c.RunOnePropagationAsync(
                It.IsAny<string>(),
                It.IsAny<RunSimulationRequest>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunPropagationOutcome(simulated, Array.Empty<string>()));

        var handler = new WhatIfSimulationQueryHandler(runCommand.Object, assetRepo.Object, relRepo.Object);

        var result = await handler.RunAsync(new RunSimulationRequest
        {
            TriggerAssetId = "a1",
            MaxDepth = 3
        });

        Assert.Empty(result.Deltas);
    }
}
