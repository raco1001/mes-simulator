using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Domain.Simulation;
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
        Assert.Equal(SimulationEngineConstants.DefaultEngineTickIntervalMs, capturedDto.EngineTickIntervalMs);
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
                UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(-1)
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
        var temp = Convert.ToDouble(capturedState!.Properties["temp"]);
        Assert.InRange(temp, 11.99, 12.02);
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

    [Fact]
    public async Task RunOnePropagationAsync_MultiSeed_ProcessesEachSeed()
    {
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
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object);

        await handler.RunOnePropagationAsync(
            "run-1",
            new RunSimulationRequest { TriggerAssetIds = ["seed-a", "seed-b"], MaxDepth = 3 },
            cancellationToken: CancellationToken.None);

        mockAssetRepo.Verify(r => r.GetStateByAssetIdAsync("seed-a", It.IsAny<CancellationToken>()), Times.Once);
        mockAssetRepo.Verify(r => r.GetStateByAssetIdAsync("seed-b", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunOnePropagationAsync_FanIn_DistinctMappedKeys_MergesOnConvergedNode()
    {
        var states = new Dictionary<string, StateDto>();
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = id == "gateway" ? "Gateway" : "Drone",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
            {
                return id switch
                {
                    "drone-a" => new StateDto
                    {
                        AssetId = "drone-a",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1000d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "drone-b" => new StateDto
                    {
                        AssetId = "drone-b",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1200d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "drone-c" => new StateDto
                    {
                        AssetId = "drone-c",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1300d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    _ => states.TryGetValue(id, out var s) ? s : null
                };
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => states[s.AssetId] = s)
            .Returns(Task.CompletedTask);

        var t = DateTimeOffset.UtcNow;
        RelationshipDto Rel(string from, string to, string toProp) => new()
        {
            Id = $"r-{from}-{to}",
            FromAssetId = from,
            ToAssetId = to,
            RelationshipType = "Supplies",
            Mappings = [new PropertyMapping("streamOut", toProp)],
            Properties = new Dictionary<string, object>(),
            CreatedAt = t,
            UpdatedAt = t
        };

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-a", "gateway", "streamInput_a")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-b", "gateway", "streamInput_b")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-c", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-c", "gateway", "streamInput_c")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("gateway", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.Is<string>(x => x != "drone-a" && x != "drone-b" && x != "drone-c" && x != "gateway"), It.IsAny<CancellationToken>()))
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
            objectTypeSchemaRepository: mockObjectTypeRepo.Object,
            propagationRules:
            [
                new SuppliesRule(),
                new ContainsRule(),
                new ConnectedToRule(),
            ]);

        await handler.RunOnePropagationAsync(
            "run-1",
            new RunSimulationRequest { TriggerAssetIds = ["drone-a", "drone-b", "drone-c"], MaxDepth = 5 },
            cancellationToken: CancellationToken.None);

        Assert.True(states.ContainsKey("gateway"));
        var g = states["gateway"].Properties;
        Assert.Equal(1000d, Convert.ToDouble(g["streamInput_a"]));
        Assert.Equal(1200d, Convert.ToDouble(g["streamInput_b"]));
        Assert.Equal(1300d, Convert.ToDouble(g["streamInput_c"]));
    }

    /// <summary>
    /// Persisted state keys match Mongo camelCase (stream_in_1 → streamIn1). Fan-in must not zero other inputs.
    /// </summary>
    [Fact]
    public async Task RunOnePropagationAsync_FanIn_WithSchema_ResolvesPersistedCamelCasePropertyKeys()
    {
        var storedStates = new Dictionary<string, StateDto>();
        var t = DateTimeOffset.UtcNow;
        var gatewaySchema = new ObjectTypeSchemaDto
        {
            SchemaVersion = "v1",
            ObjectType = "Gateway",
            DisplayName = "Gateway",
            Traits = new ObjectTraits
            {
                Persistence = Persistence.Durable,
                Dynamism = Dynamism.Dynamic,
                Cardinality = Cardinality.Enumerable,
            },
            OwnProperties =
            [
                new PropertyDefinition
                {
                    Key = "stream_in_1",
                    DataType = DataType.Number,
                    SimulationBehavior = SimulationBehavior.Settable,
                    Mutability = Mutability.Mutable,
                    Required = false,
                },
                new PropertyDefinition
                {
                    Key = "stream_in_2",
                    DataType = DataType.Number,
                    SimulationBehavior = SimulationBehavior.Settable,
                    Mutability = Mutability.Mutable,
                    Required = false,
                },
                new PropertyDefinition
                {
                    Key = "stream_in_3",
                    DataType = DataType.Number,
                    SimulationBehavior = SimulationBehavior.Settable,
                    Mutability = Mutability.Mutable,
                    Required = false,
                },
                new PropertyDefinition
                {
                    Key = "stream_out",
                    DataType = DataType.Number,
                    SimulationBehavior = SimulationBehavior.Derived,
                    Mutability = Mutability.Mutable,
                    Required = false,
                    Constraints = new Dictionary<string, object?>
                    {
                        ["dependsOn"] = new List<object?> { "stream_in_1", "stream_in_2", "stream_in_3" },
                        ["operation"] = "sum",
                    },
                },
            ],
            CreatedAt = t,
            UpdatedAt = t,
        };

        static StateDto PersistLikeMongo(StateDto s)
        {
            var props = new Dictionary<string, object?>();
            foreach (var kv in s.Properties)
            {
                if (kv.Value is null) continue;
                props[SimulationPropertyKeyNormalizer.ToPersistenceKey(kv.Key)] = kv.Value;
            }

            return new StateDto
            {
                AssetId = s.AssetId,
                Properties = props,
                Status = s.Status,
                UpdatedAt = s.UpdatedAt,
            };
        }

        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = id == "gateway" ? "Gateway" : "Drone",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
            {
                return id switch
                {
                    "drone-a" => new StateDto
                    {
                        AssetId = "drone-a",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1000d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow,
                    },
                    "drone-b" => new StateDto
                    {
                        AssetId = "drone-b",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1200d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow,
                    },
                    "drone-c" => new StateDto
                    {
                        AssetId = "drone-c",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1300d },
                        Status = "normal",
                        UpdatedAt = DateTimeOffset.UtcNow,
                    },
                    _ => storedStates.TryGetValue(id, out var s) ? s : null,
                };
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => storedStates[s.AssetId] = PersistLikeMongo(s))
            .Returns(Task.CompletedTask);

        RelationshipDto Rel(string from, string to, string toProp) => new()
        {
            Id = $"r-{from}-{to}",
            FromAssetId = from,
            ToAssetId = to,
            RelationshipType = "Supplies",
            Mappings = [new PropertyMapping("streamOut", toProp)],
            Properties = new Dictionary<string, object>(),
            CreatedAt = t,
            UpdatedAt = t,
        };

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-a", "gateway", "stream_in_1")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-b", "gateway", "stream_in_2")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-c", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("drone-c", "gateway", "stream_in_3")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("gateway", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.Is<string>(x => x != "drone-a" && x != "drone-b" && x != "drone-c" && x != "gateway"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());

        var mockEventRepo = new Mock<IEventRepository>();
        mockEventRepo.Setup(r => r.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockPublisher = new Mock<IEventPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockObjectTypeRepo = new Mock<IObjectTypeSchemaRepository>();
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync("Gateway", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gatewaySchema);
        mockObjectTypeRepo.Setup(r => r.GetByObjectTypeAsync(It.Is<string>(ot => ot != "Gateway"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = CreateHandler(
            assetRepository: mockAssetRepo.Object,
            relationshipRepository: mockRelRepo.Object,
            eventRepository: mockEventRepo.Object,
            eventPublisher: mockPublisher.Object,
            objectTypeSchemaRepository: mockObjectTypeRepo.Object,
            propagationRules:
            [
                new SuppliesRule(),
                new ContainsRule(),
                new ConnectedToRule(),
            ]);

        await handler.RunOnePropagationAsync(
            "run-1",
            new RunSimulationRequest { TriggerAssetIds = ["drone-a", "drone-b", "drone-c"], MaxDepth = 5 },
            cancellationToken: CancellationToken.None);

        Assert.True(storedStates.ContainsKey("gateway"));
        var g = storedStates["gateway"].Properties;
        Assert.Equal(1000d, Convert.ToDouble(g["streamIn1"]));
        Assert.Equal(1200d, Convert.ToDouble(g["streamIn2"]));
        Assert.Equal(1300d, Convert.ToDouble(g["streamIn3"]));
        Assert.Equal(3500d, Convert.ToDouble(g["streamOut"]));
    }

    /// <summary>
    /// Region applies power_in first (stale stream_in_1 left from DB); MQTT fan-in must not sum stale + absolute patch.
    /// Queue order with seeds [region, mqtt] is: region, mqtt, lc(power), lc(stream) so LC is reached via power first.
    /// </summary>
    [Fact]
    public async Task RunOnePropagationAsync_FanIn_StaleStreamIn1_NotSummedWhenFirstVisitDidNotTouchStream()
    {
        var states = new Dictionary<string, StateDto>();
        var t = DateTimeOffset.UtcNow;
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = "Test",
                CreatedAt = t,
                UpdatedAt = t
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
            {
                return id switch
                {
                    "region-1" => new StateDto
                    {
                        AssetId = "region-1",
                        Properties = new Dictionary<string, object?> { ["power"] = 1000d },
                        Status = "normal",
                        UpdatedAt = t
                    },
                    "mqtt-1" => new StateDto
                    {
                        AssetId = "mqtt-1",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 3500d },
                        Status = "normal",
                        UpdatedAt = t
                    },
                    "lc-1" => states.TryGetValue("lc-1", out var s)
                        ? s
                        : new StateDto
                        {
                            AssetId = "lc-1",
                            Properties = new Dictionary<string, object?>
                            {
                                ["stream_in_1"] = 5400d,
                                ["power_in"] = 0d
                            },
                            Status = "normal",
                            UpdatedAt = t
                        },
                    _ => null
                };
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => states[s.AssetId] = s)
            .Returns(Task.CompletedTask);

        RelationshipDto Rel(string id, string from, string to, string fromProp, string toProp) => new()
        {
            Id = id,
            FromAssetId = from,
            ToAssetId = to,
            RelationshipType = "Supplies",
            Mappings = [new PropertyMapping(fromProp, toProp)],
            Properties = new Dictionary<string, object>(),
            CreatedAt = t,
            UpdatedAt = t
        };

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync("region-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("r-lc", "region-1", "lc-1", "power", "power_in")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("mqtt-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("m-lc", "mqtt-1", "lc-1", "streamOut", "stream_in_1")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("lc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.Is<string>(x => x != "region-1" && x != "mqtt-1" && x != "lc-1"), It.IsAny<CancellationToken>()))
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
            objectTypeSchemaRepository: mockObjectTypeRepo.Object,
            propagationRules:
            [
                new SuppliesRule(),
                new ContainsRule(),
                new ConnectedToRule(),
            ]);

        await handler.RunOnePropagationAsync(
            "run-1",
            new RunSimulationRequest { TriggerAssetIds = ["region-1", "mqtt-1"], MaxDepth = 5 },
            cancellationToken: CancellationToken.None);

        Assert.True(states.ContainsKey("lc-1"));
        var lc = states["lc-1"].Properties;
        Assert.Equal(3500d, Convert.ToDouble(lc["stream_in_1"]));
        Assert.Equal(1000d, Convert.ToDouble(lc["power_in"]));
    }

    [Fact]
    public async Task RunOnePropagationAsync_FanIn_SameKeyTwoSuppliers_StillSums()
    {
        var states = new Dictionary<string, StateDto>();
        var t = DateTimeOffset.UtcNow;
        var mockAssetRepo = new Mock<IAssetRepository>();
        mockAssetRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = "Test",
                CreatedAt = t,
                UpdatedAt = t
            });
        mockAssetRepo.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
            {
                return id switch
                {
                    "drone-a" => new StateDto
                    {
                        AssetId = "drone-a",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1000d },
                        Status = "normal",
                        UpdatedAt = t
                    },
                    "drone-b" => new StateDto
                    {
                        AssetId = "drone-b",
                        Properties = new Dictionary<string, object?> { ["streamOut"] = 1200d },
                        Status = "normal",
                        UpdatedAt = t
                    },
                    "lc-1" => states.TryGetValue("lc-1", out var s) ? s : null,
                    _ => null
                };
            });
        mockAssetRepo.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => states[s.AssetId] = s)
            .Returns(Task.CompletedTask);

        RelationshipDto Rel(string id, string from, string to) => new()
        {
            Id = id,
            FromAssetId = from,
            ToAssetId = to,
            RelationshipType = "Supplies",
            Mappings = [new PropertyMapping("streamOut", "stream_in_1")],
            Properties = new Dictionary<string, object>(),
            CreatedAt = t,
            UpdatedAt = t
        };

        var mockRelRepo = new Mock<IRelationshipRepository>();
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("a-lc", "drone-a", "lc-1")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("drone-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Rel("b-lc", "drone-b", "lc-1")]);
        mockRelRepo.Setup(r => r.GetOutgoingAsync("lc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RelationshipDto>());
        mockRelRepo.Setup(r => r.GetOutgoingAsync(It.Is<string>(x => x != "drone-a" && x != "drone-b" && x != "lc-1"), It.IsAny<CancellationToken>()))
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
            objectTypeSchemaRepository: mockObjectTypeRepo.Object,
            propagationRules:
            [
                new SuppliesRule(),
                new ContainsRule(),
                new ConnectedToRule(),
            ]);

        await handler.RunOnePropagationAsync(
            "run-1",
            new RunSimulationRequest { TriggerAssetIds = ["drone-a", "drone-b"], MaxDepth = 5 },
            cancellationToken: CancellationToken.None);

        Assert.True(states.ContainsKey("lc-1"));
        Assert.Equal(2200d, Convert.ToDouble(states["lc-1"].Properties["stream_in_1"]));
    }

    [Fact]
    public void SimulationPropertyKeyNormalizer_ToPersistenceKey_stream_in_1_matches_mongo_convention()
    {
        Assert.Equal("streamIn1", SimulationPropertyKeyNormalizer.ToPersistenceKey("stream_in_1"));
        Assert.Equal("streamOut", SimulationPropertyKeyNormalizer.ToPersistenceKey("stream_out"));
    }

    /// <summary>
    /// Supplies(supplier → orchestrator) applies only when supplier is dequeued; downstream-only seed skips that edge.
    /// </summary>
    [Fact]
    public async Task RunOnePropagation_SeedOnlyDownstream_UpstreamSuppliesNotApplied()
    {
        const string supplier = "center-supplier";
        const string orchestrator = "orch-server";

        var supplies = new RelationshipDto
        {
            Id = "rel-sup-orch",
            FromAssetId = supplier,
            ToAssetId = orchestrator,
            RelationshipType = "Supplies",
            Mappings =
            [
                new PropertyMapping("powerOut", "powerIn", "value", "kW", "kW"),
            ],
            Properties = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var mockRel = new Mock<IRelationshipRepository>();
        mockRel
            .Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                id == supplier ? new List<RelationshipDto> { supplies } : Array.Empty<RelationshipDto>());

        var supplierState = new StateDto
        {
            AssetId = supplier,
            Properties = new Dictionary<string, object?> { ["powerOut"] = 5000d },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var mockAsset = new Mock<IAssetRepository>();
        mockAsset.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                id == supplier ? supplierState : (StateDto?)null);
        mockAsset.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = "test",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        mockAsset.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRunRepo = CreateMockRunRepository();
        var handler = CreateHandler(
            assetRepository: mockAsset.Object,
            relationshipRepository: mockRel.Object,
            simulationRunRepository: mockRunRepo.Object,
            propagationRules: [new SuppliesRule(), new ContainsRule(), new ConnectedToRule()]);

        var outcome = await handler.RunOnePropagationAsync(
            "run-verify-seed",
            new RunSimulationRequest { TriggerAssetId = orchestrator, MaxDepth = 10 },
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.States.TryGetValue(orchestrator, out var orchMerged));
        Assert.False(orchMerged.Properties.ContainsKey("powerIn"));
    }

    [Fact]
    public async Task RunOnePropagation_SeedIncludesSupplier_SuppliesAppliesPowerInToOrchestrator()
    {
        const string supplier = "center-supplier";
        const string orchestrator = "orch-server";

        var supplies = new RelationshipDto
        {
            Id = "rel-sup-orch",
            FromAssetId = supplier,
            ToAssetId = orchestrator,
            RelationshipType = "Supplies",
            Mappings =
            [
                new PropertyMapping("powerOut", "powerIn", "value", "kW", "kW"),
            ],
            Properties = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var mockRel = new Mock<IRelationshipRepository>();
        mockRel
            .Setup(r => r.GetOutgoingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                id == supplier ? new List<RelationshipDto> { supplies } : Array.Empty<RelationshipDto>());

        var supplierState = new StateDto
        {
            AssetId = supplier,
            Properties = new Dictionary<string, object?> { ["powerOut"] = 5000d },
            Status = "normal",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var mockAsset = new Mock<IAssetRepository>();
        mockAsset.Setup(r => r.GetStateByAssetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                id == supplier ? supplierState : (StateDto?)null);
        mockAsset.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new AssetDto
            {
                Id = id,
                Type = "test",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        mockAsset.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRunRepo = CreateMockRunRepository();
        var handler = CreateHandler(
            assetRepository: mockAsset.Object,
            relationshipRepository: mockRel.Object,
            simulationRunRepository: mockRunRepo.Object,
            propagationRules: [new SuppliesRule(), new ContainsRule(), new ConnectedToRule()]);

        var outcome = await handler.RunOnePropagationAsync(
            "run-verify-seed",
            new RunSimulationRequest { TriggerAssetId = supplier, MaxDepth = 10 },
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.States.TryGetValue(orchestrator, out var orchMerged));
        Assert.True(orchMerged.Properties.TryGetValue("powerIn", out var p));
        Assert.Equal(5000d, Convert.ToDouble(p));
    }

    private static RunSimulationCommandHandler CreateHandler(
        IAssetRepository? assetRepository = null,
        IRelationshipRepository? relationshipRepository = null,
        ISimulationRunRepository? simulationRunRepository = null,
        IEngineStateApplier? applier = null,
        IEventRepository? eventRepository = null,
        IEventPublisher? eventPublisher = null,
        IObjectTypeSchemaRepository? objectTypeSchemaRepository = null,
        IEnumerable<IPropagationRule>? propagationRules = null)
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
            propagationRules ?? Array.Empty<IPropagationRule>(),
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
        m.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SimulationRunDto?)null);
        m.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<SimulationRunStatus>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m.Setup(r => r.UpdateTickIndexAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m.Setup(r => r.EndAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return m;
    }
}
