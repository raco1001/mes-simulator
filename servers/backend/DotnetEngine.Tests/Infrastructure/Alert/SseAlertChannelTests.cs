using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Infrastructure.Alert;
using Xunit;

namespace DotnetEngine.Tests.Infrastructure.Alert;

public class SseAlertChannelTests
{
    [Fact]
    public async Task NotifyAsync_PublishesAlertToActiveSubscriber()
    {
        var notifier = new SseAlertChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await using var subscriber = notifier.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var moveNextTask = subscriber.MoveNextAsync().AsTask();
        await notifier.NotifyAsync(CreateAlert("freezer-1"), CancellationToken.None);

        var completed = await Task.WhenAny(moveNextTask, Task.Delay(TimeSpan.FromSeconds(2), cts.Token));
        Assert.Same(moveNextTask, completed);
        Assert.True(await moveNextTask);
        Assert.Equal("freezer-1", subscriber.Current.AssetId);
    }

    [Fact]
    public async Task SubscribeAsync_MultipleSubscribersEachReceivePublishedAlert()
    {
        var notifier = new SseAlertChannel();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await using var subscriber1 = notifier.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await using var subscriber2 = notifier.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var move1 = subscriber1.MoveNextAsync().AsTask();
        var move2 = subscriber2.MoveNextAsync().AsTask();

        await notifier.NotifyAsync(CreateAlert("freezer-2"), CancellationToken.None);

        Assert.True(await move1);
        Assert.True(await move2);
        Assert.Equal("freezer-2", subscriber1.Current.AssetId);
        Assert.Equal("freezer-2", subscriber2.Current.AssetId);
    }

    private static AlertDto CreateAlert(string assetId) =>
        new()
        {
            AssetId = assetId,
            Timestamp = DateTimeOffset.UtcNow,
            Severity = "warning",
            Message = "temperature high",
            Metadata = new Dictionary<string, object>(),
        };
}
