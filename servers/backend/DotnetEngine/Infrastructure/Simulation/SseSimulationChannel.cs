using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotnetEngine.Application.Simulation.Ports.Driven;

namespace DotnetEngine.Infrastructure.Simulation;

/// <summary>
/// SSE 구독 연결별 Channel을 관리하는 Simulation notifier 구현체.
/// Alert SSE와 동일한 패턴.
/// </summary>
public sealed class SseSimulationChannel : ISimulationNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<SimulationTickEvent>> _subscribers = new();

    public async Task NotifyAsync(SimulationTickEvent tickEvent, CancellationToken ct)
    {
        var snapshot = _subscribers.ToArray();
        foreach (var (subscriberId, channel) in snapshot)
        {
            try
            {
                await channel.Writer.WriteAsync(tickEvent, ct);
            }
            catch
            {
                RemoveSubscriber(subscriberId, channel);
            }
        }
    }

    public async IAsyncEnumerable<SimulationTickEvent> SubscribeAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<SimulationTickEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        _subscribers[subscriberId] = channel;
        using var registration = ct.Register(() => RemoveSubscriber(subscriberId, channel));

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                yield return evt;
            }
        }
        finally
        {
            RemoveSubscriber(subscriberId, channel);
        }
    }

    private void RemoveSubscriber(Guid subscriberId, Channel<SimulationTickEvent> channel)
    {
        _subscribers.TryRemove(new KeyValuePair<Guid, Channel<SimulationTickEvent>>(subscriberId, channel));
        channel.Writer.TryComplete();
    }
}
