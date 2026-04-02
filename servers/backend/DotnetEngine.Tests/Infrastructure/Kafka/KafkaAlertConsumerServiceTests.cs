using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using DotnetEngine.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Infrastructure.Kafka;

public class KafkaAlertConsumerServiceTests
{
    private static IServiceScopeFactory CreateScopeFactory(IAlertStore alertStore)
    {
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(IAlertStore))).Returns(alertStore);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(sp.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        return factory.Object;
    }

    [Fact]
    public async Task ProcessMessageAsync_StoresAndNotifies_WhenAlertGeneratedEvent()
    {
        var alertStore = new Mock<IAlertStore>();
        var alertNotifier = new Mock<IAlertNotifier>();
        var logger = new Mock<ILogger<KafkaAlertConsumerService>>();
        var options = Options.Create(new KafkaOptions());
        var scopeFactory = CreateScopeFactory(alertStore.Object);
        var sut = new KafkaAlertConsumerService(scopeFactory, alertNotifier.Object, options, logger.Object);

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
        var scopeFactory = CreateScopeFactory(alertStore.Object);
        var sut = new KafkaAlertConsumerService(scopeFactory, alertNotifier.Object, options, logger.Object);

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

    [Fact]
    public async Task ProcessMessageAsync_ParsesMetricsArrayAndMapsFirstMetric()
    {
        var alertStore = new Mock<IAlertStore>();
        var alertNotifier = new Mock<IAlertNotifier>();
        var logger = new Mock<ILogger<KafkaAlertConsumerService>>();
        var options = Options.Create(new KafkaOptions());
        var scopeFactory = CreateScopeFactory(alertStore.Object);
        var sut = new KafkaAlertConsumerService(scopeFactory, alertNotifier.Object, options, logger.Object);

        AlertDto? notifiedAlert = null;
        alertNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<AlertDto>(), It.IsAny<CancellationToken>()))
            .Callback<AlertDto, CancellationToken>((alert, _) => notifiedAlert = alert)
            .Returns(Task.CompletedTask);

        var json = """
                   {
                     "assetId": "battery-1",
                     "timestamp": "2026-03-25T10:20:30Z",
                     "payload": {
                       "severity": "error",
                       "message": "Asset state: error",
                       "metrics": [
                         {
                           "metric": "power",
                           "current": 260,
                           "threshold": 250,
                           "code": "POWER_OVERLOAD",
                           "severity": "error"
                         }
                       ]
                     }
                   }
                   """;

        await sut.ProcessMessageAsync(json, CancellationToken.None);

        Assert.NotNull(notifiedAlert);
        Assert.Single(notifiedAlert!.Metrics);
        Assert.Equal("power", notifiedAlert.Metric);
        Assert.Equal(260, notifiedAlert.Current);
        Assert.Equal(250, notifiedAlert.Threshold);
        Assert.Equal("POWER_OVERLOAD", notifiedAlert.Code);
    }
}
