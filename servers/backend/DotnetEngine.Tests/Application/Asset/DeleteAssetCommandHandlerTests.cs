using DotnetEngine.Application.Asset;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Asset;

public sealed class DeleteAssetCommandHandlerTests
{
    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenAssetMissing()
    {
        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetDto?)null);

        var relRepo = new Mock<IRelationshipRepository>();
        var handler = new DeleteAssetCommandHandler(assetRepo.Object, relRepo.Object);

        var result = await handler.DeleteAsync("a1");

        Assert.Equal(DeleteAssetResult.NotFound, result);
        relRepo.Verify(r => r.ExistsForAssetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        assetRepo.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsHasRelationships_WhenAnyEdgeExists()
    {
        var now = DateTimeOffset.UtcNow;
        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "a1",
                Type = "t",
                Connections = new List<string>(),
                Metadata = new Dictionary<string, object>(),
                CreatedAt = now,
                UpdatedAt = now,
            });

        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.ExistsForAssetAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteAssetCommandHandler(assetRepo.Object, relRepo.Object);

        var result = await handler.DeleteAsync("a1");

        Assert.Equal(DeleteAssetResult.HasRelationships, result);
        assetRepo.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsDeleted_WhenNoRelationships()
    {
        var now = DateTimeOffset.UtcNow;
        var assetRepo = new Mock<IAssetRepository>();
        assetRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDto
            {
                Id = "a1",
                Type = "t",
                Connections = new List<string>(),
                Metadata = new Dictionary<string, object>(),
                CreatedAt = now,
                UpdatedAt = now,
            });
        assetRepo.Setup(r => r.DeleteAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var relRepo = new Mock<IRelationshipRepository>();
        relRepo.Setup(r => r.ExistsForAssetAsync("a1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeleteAssetCommandHandler(assetRepo.Object, relRepo.Object);

        var result = await handler.DeleteAsync("a1");

        Assert.Equal(DeleteAssetResult.Deleted, result);
        assetRepo.Verify(r => r.DeleteAsync("a1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
