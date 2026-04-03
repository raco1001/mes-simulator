namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// 실시간 시뮬레이션 상태 전달 Port (Secondary/Driven).
/// </summary>
public interface ISimulationNotifier
{
    Task NotifyAsync(SimulationTickEvent tickEvent, CancellationToken ct);
    IAsyncEnumerable<SimulationTickEvent> SubscribeAsync(CancellationToken ct);
}

public sealed record SimulationTickEvent(
    string RunId,
    int Tick,
    string AssetId,
    IReadOnlyDictionary<string, object> Properties,
    string Status,
    DateTimeOffset Timestamp);
