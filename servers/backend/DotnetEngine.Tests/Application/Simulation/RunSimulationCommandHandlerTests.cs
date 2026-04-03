using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Domain.Simulation.ValueObjects;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation.Rules;
using DotnetEngine.Application.Simulation.Simulators;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Dto;
using Microsoft.Extensions.Logging;
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
        mockRunRepo.Setup(r => r.UpdateTickIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockRunRepo.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(simulationRunRepository: mockRunRepo.Object);
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunAsync(request, CancellationToken.None);

        Assert.NotNull(capturedDto);
        Assert.Equal(SimulationRunStatus.Pending, capturedDto.Status);
        Assert.Equal(0, capturedDto.TickIndex);
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
        mockRunRepo.Setup(r => r.UpdateTickIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        mockRunRepo.Setup(r => r.UpdateTickIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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

        await handler.RunOnePropagationAsync(runId, request, cancellationToken: CancellationToken.None);

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
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "asset-1",
                Type = "freezer",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
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
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);
        var runId = "run-1";
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3 };

        await handler.RunOnePropagationAsync(runId, request, cancellationToken: CancellationToken.None);

        mockAssetRepo.Verify(r => r.GetStateByAssetIdAsync("asset-1", It.IsAny<CancellationToken>()), Times.Once);
        mockAssetRepo.Verify(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()), Times.Once);
        mockEventRepo.Verify(r => r.AppendAsync(It.Is<EventDto>(e => e.SimulationRunId == runId), It.IsAny<CancellationToken>()), Times.Once);
        mockPublisher.Verify(p => p.PublishAsync(It.Is<EventDto>(e => e.SimulationRunId == runId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunOnePropagationAsync_EventPayload_ContainsTickFromRequestRunTick()
    {
        EventDto? capturedEvent = null;
        var mockEventRepo = new Mock<IEventRepository>();
        mockEventRepo.Setup(r => r.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Callback<EventDto, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StateDto?)null);
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "asset-1",
                Type = "freezer",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        var mockPublisher = new Mock<IEventPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);
        var runId = "run-1";
        var request = new RunSimulationRequest { TriggerAssetId = "asset-1", MaxDepth = 3, RunTick = 5 };

        await handler.RunOnePropagationAsync(runId, request, cancellationToken: CancellationToken.None);

        Assert.NotNull(capturedEvent);
        Assert.True(capturedEvent.Payload.ContainsKey("tick"));
        Assert.Equal(5, capturedEvent.Payload["tick"]);
    }

    [Fact]
    public async Task RunOnePropagationAsync_MergesPropertiesAndRemovesNullEntries()
    {
        StateDto? capturedState = null;
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "asset-1",
                Type = "freezer",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateDto
            {
                AssetId = "asset-1",
                Properties = new Dictionary<string, object?> { ["temp"] = 10, ["removeMe"] = 1 },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => capturedState = s)
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
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);

        await handler.RunOnePropagationAsync("run-1", new RunSimulationRequest
        {
            TriggerAssetId = "asset-1",
            Patch = new StatePatchDto
            {
                Properties = new Dictionary<string, object?> { ["temp"] = 20, ["removeMe"] = null }
            }
        }, cancellationToken: CancellationToken.None);

        Assert.NotNull(capturedState);
        Assert.Equal(20, capturedState!.Properties["temp"]);
        Assert.False(capturedState.Properties.ContainsKey("removeMe"));
    }

    [Fact]
    public async Task RunOnePropagationAsync_WithSchema_UsesBehaviorSimulator()
    {
        StateDto? capturedState = null;
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto { Id = "asset-1", Type = "freezer", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StateDto
            {
                AssetId = "asset-1",
                Properties = new Dictionary<string, object?> { ["temp"] = 10d },
                Status = "normal",
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => capturedState = s)
            .Returns(Task.CompletedTask);

        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync("freezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectTypeSchemaDto
            {
                SchemaVersion = "v1",
                ObjectType = "freezer",
                DisplayName = "Freezer",
                Traits = new ObjectTraits { Persistence = Persistence.Durable, Dynamism = Dynamism.Dynamic, Cardinality = Cardinality.Singular },
                OwnProperties =
                [
                    new PropertyDefinition
                    {
                        Key = "temp",
                        DataType = DataType.Number,
                        SimulationBehavior = SimulationBehavior.Rate,
                        Mutability = Mutability.Mutable,
                        BaseValue = 0d,
                        Required = true
                    }
                ]
            });

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);

        await handler.RunOnePropagationAsync("run-1", new RunSimulationRequest
        {
            TriggerAssetId = "asset-1",
            Patch = new StatePatchDto { Properties = new Dictionary<string, object?> { ["temp"] = 2d } }
        }, cancellationToken: CancellationToken.None);

        Assert.NotNull(capturedState);
        Assert.Equal(12d, capturedState!.Properties["temp"]);
    }

    [Fact]
    public async Task RunOnePropagationAsync_OnCycle_AccumulatesPatchOnce()
    {
        var states = new Dictionary<string, StateDto>();
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = "freezer",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => states.TryGetValue(id, out var state) ? state : null);
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => states[s.AssetId] = s)
            .Returns(Task.CompletedTask);

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RelationshipDto
                {
                    Id = "r1",
                    FromAssetId = "asset-1",
                    ToAssetId = "asset-2",
                    RelationshipType = "Supplies",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("asset-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RelationshipDto
                {
                    Id = "r2",
                    FromAssetId = "asset-2",
                    ToAssetId = "asset-1",
                    RelationshipType = "Supplies",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.Is<string>(x => x != "asset-1" && x != "asset-2"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var mockEventRepo = new Mock<IEventRepository>();
        mockEventRepo.Setup(r => r.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockPublisher = new Mock<IEventPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);

        await handler.RunOnePropagationAsync("run-1", new RunSimulationRequest
        {
            TriggerAssetId = "asset-1",
            Patch = new StatePatchDto
            {
                Properties = new Dictionary<string, object?> { ["flow"] = 1d }
            }
        }, cancellationToken: CancellationToken.None);

        Assert.True(states.ContainsKey("asset-1"));
        Assert.True(states.ContainsKey("asset-2"));
        Assert.Equal(2d, states["asset-1"].Properties["flow"]);
        Assert.Equal(1d, states["asset-2"].Properties["flow"]);
    }

    private static RunSimulationCommandHandler CreateHandler(
        IAssetRepository? assetRepository = null,
        IRelationshipRepository? relationshipRepository = null,
        ISimulationRunRepository? simulationRunRepository = null,
        IEngineStateApplier? applier = null,
        IEventRepository? eventRepository = null,
        IEventPublisher? eventPublisher = null,
        IObjectTypeSchemaRepository? objectTypeSchemaRepository = null)
    {
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StateDto?)null);
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "asset-1",
                Type = "freezer",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
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
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var defaultApplier = new EngineStateApplier(
            assetRepository ?? mockAssetRepo.Object,
            eventRepository ?? mockEventRepo.Object,
            eventPublisher ?? mockPublisher.Object);

        return new RunSimulationCommandHandler(
            assetRepository ?? mockAssetRepo.Object,
            relationshipRepository ?? mockRelRepo.Object,
            simulationRunRepository ?? CreateMockRunRepository().Object,
            applier ?? defaultApplier,
            eventRepository ?? mockEventRepo.Object,
            eventPublisher ?? mockPublisher.Object,
            objectTypeSchemaRepository ?? mockObjectTypeRepo.Object,
            Array.Empty<IPropagationRule>(),
            new IPropertySimulator[]
            {
                new ConstantSimulator(),
                new SettableSimulator(),
                new RateSimulator(),
                new AccumulatorSimulator(),
                new DerivedSimulator()
            },
            Mock.Of<ILogger<RunSimulationCommandHandler>>());
    }

    private static Mock<ISimulationRunRepository> CreateMockRunRepository()
    {
        var m = new Mock<ISimulationRunRepository>();
        m.Setup(r => r.CreateAsync(It.IsAny<SimulationRunDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto dto, CancellationToken _) => dto);
        m.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m.Setup(r => r.UpdateTickIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return m;
    }
}
