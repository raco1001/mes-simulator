using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class StopSimulationRunCommandHandlerTests
{
    [Fact]
    public async Task StopAsync_WhenRunExistsAndRunning_CallsUpdateStatusAsyncWithStopped()
    {
        var runId = "run-1";
        var run = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Running,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            EndedAt = null,
            TriggerAssetId = "asset-1",
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 5,
        };
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new StopSimulationRunCommandHandler(mockRunRepo.Object);

        var result = await handler.StopAsync(runId, CancellationToken.None);

        Assert.True(result.Success);
        mockRunRepo.Verify(
            r => r.UpdateStatusAsync(runId, SimulationRunStatus.Stopped, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenRunNotFound_ReturnsFailure()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto?)null);

        var handler = new StopSimulationRunCommandHandler(mockRunRepo.Object);

        var result = await handler.StopAsync("non-existent", CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
        mockRunRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_WhenRunAlreadyStopped_StillCallsUpdateStatusAsync_IdempotentSuccess()
    {
        var runId = "run-1";
        var run = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Stopped,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            EndedAt = DateTimeOffset.UtcNow,
            TriggerAssetId = "asset-1",
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 5,
        };
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo.Setup(r => r.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new StopSimulationRunCommandHandler(mockRunRepo.Object);

        var result = await handler.StopAsync(runId, CancellationToken.None);

        Assert.True(result.Success);
        mockRunRepo.Verify(
            r => r.UpdateStatusAsync(runId, SimulationRunStatus.Stopped, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
