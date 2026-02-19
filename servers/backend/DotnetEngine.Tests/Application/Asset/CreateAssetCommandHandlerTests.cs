using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Asset;

public class CreateAssetCommandHandlerTests
{
    [Fact]
    public async Task CreateAsync_ReturnsAssetDto_WithGeneratedId()
    {
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto a, CancellationToken _) => a);

        var handler = new CreateAssetCommandHandler(mockRepository.Object);
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
    }

    [Fact]
    public async Task CreateAsync_WithEmptyConnections_StoresEmptyList()
    {
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto a, CancellationToken _) => a);

        var handler = new CreateAssetCommandHandler(mockRepository.Object);
        var request = new CreateAssetRequest { Type = "sensor", Connections = Array.Empty<string>(), Metadata = new Dictionary<string, object>() };

        var result = await handler.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(result.Connections);
        Assert.Empty(result.Connections);
    }
}
