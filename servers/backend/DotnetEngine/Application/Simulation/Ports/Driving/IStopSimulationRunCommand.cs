using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 시뮬레이션 런 중단 Command Port. Status=Stopped, EndedAt 설정.
/// </summary>
public interface IStopSimulationRunCommand
{
    Task<StopSimulationRunResult> StopAsync(string runId, CancellationToken cancellationToken = default);
}
