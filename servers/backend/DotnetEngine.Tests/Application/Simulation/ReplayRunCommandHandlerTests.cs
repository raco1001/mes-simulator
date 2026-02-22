using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Domain.Simulation.Constants;
using DotnetEngine.Domain.Simulation.ValueObjects;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class ReplayRunCommandHandlerTests
{
    [Fact]
    public async Task ReplayAsync_ReturnsFailure_WhenRunNotFound()
    {
        var runRepo = new Mock<ISimulationRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto?)null);

        var eventRepo = new Mock<IEventRepository>();
        var assetRepo = new Mock<IAssetRepository>();

        var handler = new ReplayRunCommandHandler(runRepo.Object, eventRepo.Object, assetRepo.Object);

        var result = await handler.ReplayAsync("missing", null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("missing", result.RunId);
        Assert.Equal("Run not found", result.Message);
        eventRepo.Verify(e => e.GetBySimulationRunIdAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_AppliesStateOnly_ForSimulationStateUpdatedEvents()
    {
        var run = new SimulationRunDto
        {
            Id = "run-1",
            Status = SimulationRunStatus.Stopped,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            EndedAt = DateTimeOffset.UtcNow,
            TriggerAssetId = "asset-1",
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 1,
        };
        var runRepo = new Mock<ISimulationRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var events = new List<EventDto>
        {
            new()
            {
                AssetId = "asset-1",
                EventType = EventTypes.SimulationStateUpdated,
                OccurredAt = DateTimeOffset.UtcNow,
                SimulationRunId = "run-1",
                RunTick = 0,
                Payload = new Dictionary<string, object>
                {
                    ["tick"] = 0,
                    ["status"] = "warning",
                    ["temperature"] = -5.0,
                    ["power"] = 120.0,
                },
            },
        };
        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(e => e.GetBySimulationRunIdAsync("run-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var assetRepo = new Mock<IAssetRepository>();

        var handler = new ReplayRunCommandHandler(runRepo.Object, eventRepo.Object, assetRepo.Object);

        var result = await handler.ReplayAsync("run-1", null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.EventsApplied);
        assetRepo.Verify(
            a => a.UpsertStateAsync(
                It.Is<StateDto>(s =>
                    s.AssetId == "asset-1" &&
                    s.Status == "warning" &&
                    s.CurrentTemp == -5.0 &&
                    s.CurrentPower == 120.0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplayAsync_RespectsTickMax()
    {
        var run = new SimulationRunDto
        {
            Id = "run-1",
            Status = SimulationRunStatus.Stopped,
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow,
            TriggerAssetId = "asset-1",
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 2,
        };
        var runRepo = new Mock<ISimulationRunRepository>();
        runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(e => e.GetBySimulationRunIdAsync("run-1", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventDto>());

        var assetRepo = new Mock<IAssetRepository>();
        var handler = new ReplayRunCommandHandler(runRepo.Object, eventRepo.Object, assetRepo.Object);

        await handler.ReplayAsync("run-1", 1, CancellationToken.None);

        eventRepo.Verify(e => e.GetBySimulationRunIdAsync("run-1", 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
