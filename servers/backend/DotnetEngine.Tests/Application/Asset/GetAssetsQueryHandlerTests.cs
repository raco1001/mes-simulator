using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports;
using DotnetEngine.Domain.Asset.ValueObjects;
using DotnetEngine.Infrastructure.Mongo;
using Moq;
using Xunit;
using DomainAsset = DotnetEngine.Domain.Asset.ValueObjects.Asset;

namespace DotnetEngine.Tests.Application.Asset;

public class GetAssetsQueryHandlerTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsListOfAssetDtos()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        var assets = new List<DomainAsset>
        {
            new DomainAsset
            {
                Id = "freezer-1",
                Type = "freezer",
                Connections = new List<string> { "conveyor-1" },
                Metadata = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IReadOnlyList<DomainAsset>>(assets));

        var handler = new GetAssetsQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("freezer-1", result[0].Id);
        Assert.Equal("freezer", result[0].Type);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoAssets()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IReadOnlyList<DomainAsset>>(new List<DomainAsset>()));

        var handler = new GetAssetsQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAssetDto_WhenExists()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        var asset = new DomainAsset
        {
            Id = "freezer-1",
            Type = "freezer",
            Connections = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        mockRepository
            .Setup(r => r.GetByIdAsync("freezer-1", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<DomainAsset?>(asset));

        var handler = new GetAssetsQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetByIdAsync("freezer-1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("freezer-1", result.Id);
        Assert.Equal("freezer", result.Type);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository
            .Setup(r => r.GetByIdAsync("not-found", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<DomainAsset?>(null));

        var handler = new GetAssetsQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetByIdAsync("not-found", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
