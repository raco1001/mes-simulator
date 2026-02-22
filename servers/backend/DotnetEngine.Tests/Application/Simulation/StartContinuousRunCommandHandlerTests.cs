using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation.ValueObjects;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class StartContinuousRunCommandHandlerTests
{
    [Fact]
    public async Task StartAsync_CreateAsync_ReceivesStatusRunningAndEndedAtNull()
    {
        SimulationRunDto? capturedDto = null;
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimulationRunDto>());
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .Callback<SimulationRunDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);

        var handler = new StartContinuousRunCommandHandler(mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(capturedDto);
        Assert.Equal(SimulationRunStatus.Running, capturedDto.Status);
        Assert.Null(capturedDto.EndedAt);
        Assert.Equal(0, capturedDto.TickIndex);
        Assert.Equal(capturedDto.Id, result.RunId);
    }

    [Fact]
    public async Task StartAsync_DoesNotCallRunOnePropagation_OnlyCreateAsync()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimulationRunDto>());
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);

        var handler = new StartContinuousRunCommandHandler(mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.StartAsync(request, CancellationToken.None);

        mockRunRepo.Verify(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()), Times.Once);
        mockRunRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRunRepo.Verify(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyOneRunning_ReturnsFailure()
    {
        var existingRun = new SimulationRunDto
        {
            Id = "existing-run",
            Status = SimulationRunStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = null,
            TriggerAssetId = "asset-0",
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 0,
        };
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingRun });

        var handler = new StartContinuousRunCommandHandler(mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
        mockRunRepo.Verify(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
