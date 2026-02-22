using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation.Rules;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class RunSimulationCommandHandlerTests
{
    [Fact]
    public async Task RunAsync_CreateAsync_ReceivesStatusPending()
    {
        SimulationRunDto? capturedDto = null;
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .Callback<SimulationRunDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRunRepo.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(simulationRunRepository: mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunAsync(request, CancellationToken.None);

        Assert.NotNull(capturedDto);
        Assert.Equal(SimulationRunStatus.Pending, capturedDto.Status);
    }

    [Fact]
    public async Task RunAsync_UpdateStatusAsync_CalledWithRunningAfterCreate()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        string? createdRunId = null;
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .Callback<SimulationRunDto, CancellationToken>((dto, _) => createdRunId = dto.Id)
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRunRepo.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(simulationRunRepository: mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunAsync(request, CancellationToken.None);

        mockRunRepo.Verify(
            r => r.UpdateStatusAsync(createdRunId!, SimulationRunStatus.Running, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_EndAsync_CalledOnceOnCompletion()
    {
        string? createdRunId = null;
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo
            .Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .Callback<SimulationRunDto, CancellationToken>((dto, _) => createdRunId = dto.Id)
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRunRepo.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(simulationRunRepository: mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunAsync(request, CancellationToken.None);

        mockRunRepo.Verify(
            r => r.EndAsync(createdRunId!, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunOnePropagationAsync_WhenRunExists_DoesNotCreateOrEndRun()
    {
        var mockRunRepo = new Mock<ISimulationRunRepository>();
        mockRunRepo.Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        mockRunRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRunRepo.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(simulationRunRepository: mockRunRepo.Object);
        var runId = "existing-run-id";
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunOnePropagationAsync(runId, request, CancellationToken.None);

        mockRunRepo.Verify(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRunRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRunRepo.Verify(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnePropagationAsync_ExecutesPropagation_AppendsEventAndUpsertsState()
    {
        var mockEventRepo = new Mock<IEventRepository>();
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StateDto?)null);
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mockEventRepo.Setup(r => r.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockPublisher = new Mock<IEventPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object);
        var runId = "run-1";
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunOnePropagationAsync(runId, request, CancellationToken.None);

        mockAssetRepo.Verify(r => r.GetStateByAssetIdAsync("asset-1", It.IsAny<CancellationToken>()), Times.Once);
        mockAssetRepo.Verify(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()), Times.Once);
        mockEventRepo.Verify(r => r.AppendAsync(It.Is<EventDto>(e => e.SimulationRunId == runId), It.IsAny<CancellationToken>()), Times.Once);
        mockPublisher.Verify(p => p.PublishAsync(It.Is<EventDto>(e => e.SimulationRunId == runId), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RunSimulationCommandHandler CreateHandler(
        IAssetRepository? assetRepository = null,
        IRelationshipRepository? relationshipRepository = null,
        ISimulationRunRepository? simulationRunRepository = null,
        IEventRepository? eventRepository = null,
        IEventPublisher? eventPublisher = null)
    {
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StateDto?)null);
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var mockEventRepo = new Mock<IEventRepository>();
        mockEventRepo.Setup(r => r.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockPublisher = new Mock<IEventPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new RunSimulationCommandHandler(
            assetRepository ?? mockAssetRepo.Object,
            relationshipRepository ?? mockRelRepo.Object,
            simulationRunRepository ?? CreateMockRunRepository().Object,
            eventRepository ?? mockEventRepo.Object,
            eventPublisher ?? mockPublisher.Object,
            Array.Empty<IPropagationRule>());
    }

    private static Mock<ISimulationRunRepository> CreateMockRunRepository()
    {
        var m = new Mock<ISimulationRunRepository>();
        m.Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        m.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return m;
    }
}
