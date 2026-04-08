using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
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
    private static StartContinuousRunCommandHandler CreateHandler(
        Mock<ISimulationRunRepository> mockRunRepo,
        Mock<IAssetRepository>? mockAssetRepo = null,
        Mock<IRunSimulationCommand>? mockRunCmd = null)
    {
        var assetRepo = mockAssetRepo ?? new Mock<IAssetRepository>();
        assetRepo
            .Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DotnetEngine.Application.Asset.Dto.StateDto?)null);
        assetRepo
            .Setup(r => r.DeleteStatesByAssetIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo
            .Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var runCmd = mockRunCmd ?? new Mock<IRunSimulationCommand>();
        if (mockRunCmd is null)
        {
            runCmd
                .Setup(r => r.BuildBaselineInitialSnapshotAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, object>());
        }

        mockRunRepo
            .Setup(r => r.ReplaceInitialSnapshotAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new StartContinuousRunCommandHandler(mockRunRepo.Object, assetRepo.Object, mockRelRepo.Object, runCmd.Object);
    }

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

        var handler = CreateHandler(mockRunRepo);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(capturedDto);
        Assert.Equal(SimulationRunStatus.Running, capturedDto.Status);
        Assert.Null(capturedDto.EndedAt);
        Assert.Equal(0, capturedDto.TickIndex);
        Assert.Equal(capturedDto.Id, result.RunId);
        Assert.Equal("asset-1", capturedDto.TriggerAssetId);
        Assert.Single(capturedDto.TriggerAssetIds);
        Assert.Equal("asset-1", capturedDto.TriggerAssetIds[0]);
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

        var handler = CreateHandler(mockRunRepo);
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
            TriggerAssetIds = ["asset-0"],
            Trigger = new Dictionary<string, object>(),
            MaxDepth = 3,
            TickIndex = 0,
        };
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingRun });

        var handler = CreateHandler(mockRunRepo);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
        mockRunRepo.Verify(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithMultipleTriggerAssetIds_PersistsAllSeeds_Deduped()
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

        var handler = CreateHandler(mockRunRepo);
        var request = new RunSimulationRequest
        {
            TriggerAssetIds = ["  asset-a ", "asset-b", "asset-a"],
            MaxDepth = 3,
        };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(capturedDto);
        Assert.Equal(new[] { "asset-a", "asset-b" }, capturedDto!.TriggerAssetIds);
    }

    [Fact]
    public async Task StartAsync_WithMultipleSeedsAndMeaningfulPatch_ReturnsFailure()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimulationRunDto>());

        var handler = CreateHandler(mockRunRepo);
        var request = new RunSimulationRequest
        {
            TriggerAssetIds = ["a", "b"],
            Patch = new StatePatchDto { Status = "warning" },
        };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
        mockRunRepo.Verify(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenResetState_DeletesPersistedStates_AndUsesBaselineSnapshot()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.GetRunningAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimulationRunDto>());
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);

        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo
            .Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DotnetEngine.Application.Asset.Dto.StateDto?)null);
        mockAssetRepo
            .Setup(r => r.DeleteStatesByAssetIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRunCmd = new Mock<IRunSimulationCommand>();
        var baseline = new Dictionary<string, object>
        {
            ["seed-1"] = new Dictionary<string, object>
            {
                ["properties"] = new Dictionary<string, object> { ["p"] = 1d },
                ["simulationStatus"] = "normal",
            },
        };
        mockRunCmd
            .Setup(r => r.BuildBaselineInitialSnapshotAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        var handler = CreateHandler(mockRunRepo, mockAssetRepo, mockRunCmd);
        var request = new RunSimulationRequest { TriggerAssetId = "seed-1", MaxDepth = 3, ResetState = true };

        var result = await handler.StartAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        mockAssetRepo.Verify(
            r => r.DeleteStatesByAssetIdsAsync(
                It.Is<IReadOnlyList<string>>(ids => ids.Contains("seed-1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockRunCmd.Verify(
            r => r.BuildBaselineInitialSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("seed-1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockRunRepo.Verify(
            r => r.ReplaceInitialSnapshotAsync(
                It.IsAny<string>(),
                It.Is<IReadOnlyDictionary<string, object>>(d => d.ContainsKey("seed-1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
