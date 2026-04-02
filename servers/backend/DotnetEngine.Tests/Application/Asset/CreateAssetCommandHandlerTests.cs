using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Asset;

public class CreateAssetCommandHandlerTests
{
    [Fact]
    public async Task CreateAsync_ReturnsAssetDto_WithGeneratedId()
    {
        var mockRepository = new Mock<IAssetRepository>();
        var mockObjectTypeRepository = new Mock<IObjectTypeSchemaRepository>();
        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto a, CancellationToken _) => a);
        mockRepository
            .Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockObjectTypeRepository
            .Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = new CreateAssetCommandHandler(mockRepository.Object, mockObjectTypeRepository.Object);
        var request = new CreateAssetRequest
        {
            Type = "freezer",
            Connections = new List<string> { "c1" },
            Metadata = new Dictionary<string, object>()
        };

        var result = await handler.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.Equal("freezer", result.Type);
        Assert.Single(result.Connections);
        Assert.Equal("c1", result.Connections[0]);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()), Times.Once);
        mockRepository.Verify(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyConnections_StoresEmptyList()
    {
        var mockRepository = new Mock<IAssetRepository>();
        var mockObjectTypeRepository = new Mock<IObjectTypeSchemaRepository>();
        mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto a, CancellationToken _) => a);
        mockRepository
            .Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockObjectTypeRepository
            .Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectTypeSchemaDto?)null);

        var handler = new CreateAssetCommandHandler(mockRepository.Object, mockObjectTypeRepository.Object);
        var request = new CreateAssetRequest { Type = "sensor", Connections = Array.Empty<string>(), Metadata = new Dictionary<string, object>() };

        var result = await handler.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(result.Connections);
        Assert.Empty(result.Connections);
    }

    [Fact]
    public async Task CreateAsync_WhenObjectTypeSchemaExists_InitializesStatePropertiesWithBaseValues()
    {
        StateDto? capturedState = null;
        var mockRepository = new Mock<IAssetRepository>();
        var mockObjectTypeRepository = new Mock<IObjectTypeSchemaRepository>();
        mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto a, CancellationToken _) => a);
        mockRepository.Setup(r => r.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()))
            .Callback<StateDto, CancellationToken>((s, _) => capturedState = s)
            .Returns(Task.CompletedTask);
        mockObjectTypeRepository.Setup(r => r.GetByObjectTypeAsync("battery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectTypeSchemaDto
            {
                SchemaVersion = "v1",
                ObjectType = "battery",
                DisplayName = "Battery",
                Traits = new ObjectTraits
                {
                    Persistence = Persistence.Durable,
                    Dynamism = Dynamism.Dynamic,
                    Cardinality = Cardinality.Enumerable
                },
                OwnProperties =
                [
                    new PropertyDefinition
                    {
                        Key = "storedEnergy",
                        DataType = DataType.Number,
                        SimulationBehavior = SimulationBehavior.Accumulator,
                        Mutability = Mutability.Mutable,
                        BaseValue = 5000,
                        Required = true
                    }
                ]
            });

        var handler = new CreateAssetCommandHandler(mockRepository.Object, mockObjectTypeRepository.Object);
        await handler.CreateAsync(new CreateAssetRequest { Type = "battery" }, CancellationToken.None);

        Assert.NotNull(capturedState);
        Assert.True(capturedState!.Properties.ContainsKey("storedEnergy"));
        Assert.Equal(5000, capturedState.Properties["storedEnergy"]);
    }
}
