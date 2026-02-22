using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 저장된 이벤트 스트림으로 상태만 재적용(Replay). 이벤트 재기록/재발행 없음.
/// </summary>
public interface IReplayRunCommand
{
    Task<ReplayRunResult> ReplayAsync(string runId, int? tickMax, CancellationToken cancellationToken = default);
}
