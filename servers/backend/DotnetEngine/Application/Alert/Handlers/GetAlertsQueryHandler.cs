using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using DotnetEngine.Application.Alert.Ports.Driving;

namespace DotnetEngine.Application.Alert.Handlers;

/// <summary>
/// 최신 알람 목록 조회 Use Case 구현.
/// </summary>
public sealed class GetAlertsQueryHandler : IGetAlertsQuery
{
    private const int DefaultLimit = 50;

    private readonly IAlertStore _alertStore;

    public GetAlertsQueryHandler(IAlertStore alertStore)
    {
        _alertStore = alertStore;
    }

    public Task<IReadOnlyList<AlertDto>> GetLatestAsync(int? limit, CancellationToken cancellationToken = default)
    {
        var maxCount = limit ?? DefaultLimit;
        var list = _alertStore.GetLatest(maxCount);
        return Task.FromResult(list);
    }
}
