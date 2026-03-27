using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using DotnetEngine.Infrastructure.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Infrastructure.Kafka;

public class KafkaAlertConsumerServiceTests
{
    [Fact]
    public async Task ProcessMessageAsync_StoresAndNotifies_WhenAlertGeneratedEvent()
    {
        var alertStore = new Mock<IAlertStore>();
        var alertNotifier = new Mock<IAlertNotifier>();
        var logger = new Mock<ILogger<KafkaAlertConsumerService>>();
        var options = Options.Create(new KafkaOptions());
        var sut = new KafkaAlertConsumerService(alertStore.Object, alertNotifier.Object, options, logger.Object);

        AlertDto? notifiedAlert = null;
        alertNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<AlertDto>(), It.IsAny<CancellationToken>()))
            .Callback<AlertDto, CancellationToken>((alert, _) => notifiedAlert = alert)
            .Returns(Task.CompletedTask);

        var json = """
                   {
                     "assetId": "freezer-1",
                     "timestamp": "2026-03-25T10:20:30Z",
                     "payload": {
                       "severity": "warning",
                       "message": "Asset state: warning",
                       "metadata": { "runId": "run-1" }
                     }
                   }
                   """;

        await sut.ProcessMessageAsync(json, CancellationToken.None);

        alertStore.Verify(s => s.Add(It.IsAny<AlertDto>()), Times.Once);
        alertNotifier.Verify(n => n.NotifyAsync(It.IsAny<AlertDto>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(notifiedAlert);
        Assert.Equal("freezer-1", notifiedAlert!.AssetId);
        Assert.Equal("warning", notifiedAlert.Severity);
    }

    [Fact]
    public async Task ProcessMessageAsync_IgnoresInvalidPayload()
    {
        var alertStore = new Mock<IAlertStore>();
        var alertNotifier = new Mock<IAlertNotifier>();
        var logger = new Mock<ILogger<KafkaAlertConsumerService>>();
        var options = Options.Create(new KafkaOptions());
        var sut = new KafkaAlertConsumerService(alertStore.Object, alertNotifier.Object, options, logger.Object);

        var json = """
                   {
                     "assetId": "freezer-1",
                     "timestamp": "2026-03-25T10:20:30Z",
                     "payload": {
                       "severity": "warning",
                       "metric": "temperature"
                     }
                   }
                   """;

        await sut.ProcessMessageAsync(json, CancellationToken.None);

        alertStore.Verify(s => s.Add(It.IsAny<AlertDto>()), Times.Never);
        alertNotifier.Verify(n => n.NotifyAsync(It.IsAny<AlertDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
