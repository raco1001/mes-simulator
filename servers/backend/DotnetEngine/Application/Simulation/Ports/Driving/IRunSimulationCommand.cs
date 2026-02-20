using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 시뮬레이션 실행 Command Port (Primary Port).
/// </summary>
public interface IRunSimulationCommand
{
    Task<RunResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default);
}
