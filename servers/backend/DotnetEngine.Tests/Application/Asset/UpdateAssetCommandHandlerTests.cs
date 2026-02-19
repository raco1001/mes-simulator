using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Asset;

public class UpdateAssetCommandHandlerTests
{
    [Fact]
    public async Task UpdateAsync_ReturnsAssetDto_WhenExists()
    {
        var now = DateTimeOffset.UtcNow;
        var existing = new AssetDto
        {
            Id = "asset-1",
            Type = "freezer",
            Connections = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            CreatedAt = now,
            UpdatedAt = now
        };
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository.Setup(r => r.GetByIdAsync("asset-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        mockRepository.Setup(r => r.UpdateAsync("asset-1", It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, AssetDto a, CancellationToken _) => a);

        var handler = new UpdateAssetCommandHandler(mockRepository.Object);
        var request = new UpdateAssetRequest { Type = "conveyor", Connections = new List<string> { "c1" }, Metadata = null };

        var result = await handler.UpdateAsync("asset-1", request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("asset-1", result.Id);
        Assert.Equal("conveyor", result.Type);
        Assert.Single(result.Connections);
        mockRepository.Verify(r => r.UpdateAsync("asset-1", It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository.Setup(r => r.GetByIdAsync("not-found", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto?)null);

        var handler = new UpdateAssetCommandHandler(mockRepository.Object);
        var request = new UpdateAssetRequest { Type = "conveyor" };

        var result = await handler.UpdateAsync("not-found", request, CancellationToken.None);

        Assert.Null(result);
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
