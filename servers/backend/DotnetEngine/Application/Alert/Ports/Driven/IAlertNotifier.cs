using DotnetEngine.Application.Alert.Dto;

namespace DotnetEngine.Application.Alert.Ports.Driven;

/// <summary>
/// 실시간 Alert 전달 Port (Secondary/Driven).
/// </summary>
public interface IAlertNotifier
{
    Task NotifyAsync(AlertDto alert, CancellationToken ct);
    IAsyncEnumerable<AlertDto> SubscribeAsync(CancellationToken ct);
}
