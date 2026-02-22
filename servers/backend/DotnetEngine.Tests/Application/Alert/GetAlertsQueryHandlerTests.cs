using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Handlers;
using DotnetEngine.Application.Alert.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Alert;

public class GetAlertsQueryHandlerTests
{
    [Fact]
    public async Task GetLatestAsync_ReturnsAlertsFromStore()
    {
        var mockStore = new Mock<IAlertStore>();
        var now = DateTimeOffset.UtcNow;
        var alerts = new List<AlertDto>
        {
            new()
            {
                AssetId = "freezer-1",
                Timestamp = now,
                Severity = "warning",
                Message = "Asset state: warning",
                Metric = "temperature",
                Current = -5.0,
                Threshold = -10,
                Code = "TEMP_HIGH",
                Metadata = new Dictionary<string, object>(),
            },
        };
        mockStore.Setup(s => s.GetLatest(50)).Returns(alerts);

        var handler = new GetAlertsQueryHandler(mockStore.Object);

        var result = await handler.GetLatestAsync(null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("freezer-1", result[0].AssetId);
        Assert.Equal("warning", result[0].Severity);
        Assert.Equal("Asset state: warning", result[0].Message);
        mockStore.Verify(s => s.GetLatest(50), Times.Once);
    }

    [Fact]
    public async Task GetLatestAsync_RespectsLimitParameter()
    {
        var mockStore = new Mock<IAlertStore>();
        mockStore.Setup(s => s.GetLatest(10)).Returns(new List<AlertDto>());

        var handler = new GetAlertsQueryHandler(mockStore.Object);

        await handler.GetLatestAsync(10, CancellationToken.None);

        mockStore.Verify(s => s.GetLatest(10), Times.Once);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsEmptyList_WhenStoreEmpty()
    {
        var mockStore = new Mock<IAlertStore>();
        mockStore.Setup(s => s.GetLatest(It.IsAny<int>())).Returns(new List<AlertDto>());

        var handler = new GetAlertsQueryHandler(mockStore.Object);

        var result = await handler.GetLatestAsync(null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
