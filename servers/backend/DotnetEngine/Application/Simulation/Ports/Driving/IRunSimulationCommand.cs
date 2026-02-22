using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 시뮬레이션 실행 Command Port (Primary Port).
/// </summary>
public interface IRunSimulationCommand
{
    Task<RunResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 주어진 Run에 대해 BFS 전파 1회만 수행. Run 생성/종료는 하지 않음. Run은 호출 전에 이미 존재한다고 가정.
    /// </summary>
    Task RunOnePropagationAsync(string runId, RunSimulationRequest request, CancellationToken cancellationToken = default);
}
