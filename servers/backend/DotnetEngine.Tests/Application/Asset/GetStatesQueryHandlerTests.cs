using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Asset;

public class GetStatesQueryHandlerTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsListOfStateDtos()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        var now = DateTimeOffset.UtcNow;
        var states = new List<StateDto>
        {
            new()
            {
                AssetId = "freezer-1",
                CurrentTemp = -5.0,
                CurrentPower = 120.0,
                Status = "normal",
                LastEventType = "asset.health.updated",
                UpdatedAt = now,
                Metadata = new Dictionary<string, object>()
            }
        };
        mockRepository.Setup(r => r.GetAllStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(states);

        var handler = new GetStatesQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("freezer-1", result[0].AssetId);
        Assert.Equal(-5.0, result[0].CurrentTemp);
        Assert.Equal(120.0, result[0].CurrentPower);
        Assert.Equal("normal", result[0].Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoStates()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository.Setup(r => r.GetAllStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StateDto>());

        var handler = new GetStatesQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByAssetIdAsync_ReturnsStateDto_WhenExists()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        var now = DateTimeOffset.UtcNow;
        var state = new StateDto
        {
            AssetId = "freezer-1",
            CurrentTemp = -5.0,
            CurrentPower = 120.0,
            Status = "normal",
            LastEventType = "asset.health.updated",
            UpdatedAt = now,
            Metadata = new Dictionary<string, object>()
        };
        mockRepository.Setup(r => r.GetStateByAssetIdAsync("freezer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        var handler = new GetStatesQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetByAssetIdAsync("freezer-1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("freezer-1", result.AssetId);
        Assert.Equal(-5.0, result.CurrentTemp);
        Assert.Equal("normal", result.Status);
    }

    [Fact]
    public async Task GetByAssetIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var mockRepository = new Mock<IAssetRepository>();
        mockRepository.Setup(r => r.GetStateByAssetIdAsync("not-found", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StateDto?)null);

        var handler = new GetStatesQueryHandler(mockRepository.Object);

        // Act
        var result = await handler.GetByAssetIdAsync("not-found", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
