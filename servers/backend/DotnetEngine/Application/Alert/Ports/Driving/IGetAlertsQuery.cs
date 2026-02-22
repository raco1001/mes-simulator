using DotnetEngine.Application.Alert.Dto;

namespace DotnetEngine.Application.Alert.Ports.Driving;

/// <summary>
/// 최신 알람 목록 조회 Port (Primary Port).
/// </summary>
public interface IGetAlertsQuery
{
    Task<IReadOnlyList<AlertDto>> GetLatestAsync(int? limit, CancellationToken cancellationToken = default);
}
