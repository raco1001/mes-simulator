using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;

namespace DotnetEngine.Infrastructure.Alert;

/// <summary>
/// SSE 구독 연결별 Channel을 관리하는 Alert notifier 구현체.
/// </summary>
public sealed class SseAlertChannel : IAlertNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<AlertDto>> _subscribers = new();

    public async Task NotifyAsync(AlertDto alert, CancellationToken ct)
    {
        var snapshot = _subscribers.ToArray();
        foreach (var (subscriberId, channel) in snapshot)
        {
            try
            {
                await channel.Writer.WriteAsync(alert, ct);
            }
            catch
            {
                // 연결이 종료된 구독자는 정리한다.
                RemoveSubscriber(subscriberId, channel);
            }
        }
    }

    public async IAsyncEnumerable<AlertDto> SubscribeAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<AlertDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        _subscribers[subscriberId] = channel;
        using var registration = ct.Register(() => RemoveSubscriber(subscriberId, channel));

        try
        {
            await foreach (var alert in channel.Reader.ReadAllAsync(ct))
            {
                yield return alert;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId, channel);
        }
    }

    private void RemoveSubscriber(Guid subscriberId, Channel<AlertDto> channel)
    {
        _subscribers.TryRemove(new KeyValuePair<Guid, Channel<AlertDto>>(subscriberId, channel));
        channel.Writer.TryComplete();
    }
}
