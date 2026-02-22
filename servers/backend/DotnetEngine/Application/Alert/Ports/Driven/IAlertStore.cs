using DotnetEngine.Application.Alert.Dto;

namespace DotnetEngine.Application.Alert.Ports.Driven;

/// <summary>
/// 알람 저장소 Port (Secondary/Driven). In-memory 최근 N건 유지.
/// </summary>
public interface IAlertStore
{
    void Add(AlertDto alert);
    IReadOnlyList<AlertDto> GetLatest(int maxCount);
}
