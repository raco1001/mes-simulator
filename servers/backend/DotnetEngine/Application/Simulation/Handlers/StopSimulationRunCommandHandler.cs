using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 시뮬레이션 런 중단 Use Case. Status=Stopped, EndedAt=UtcNow 설정.
/// </summary>
public sealed class StopSimulationRunCommandHandler : IStopSimulationRunCommand
{
    private readonly ISimulationRunRepository _simulationRunRepository;

    public StopSimulationRunCommandHandler(ISimulationRunRepository simulationRunRepository)
    {
        _simulationRunRepository = simulationRunRepository;
    }

    public async Task<StopSimulationRunResult> StopAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return new StopSimulationRunResult
            {
                Success = false,
                Message = "Simulation run not found.",
            };

        var endedAt = DateTimeOffset.UtcNow;
        await _simulationRunRepository.UpdateStatusAsync(runId, SimulationRunStatus.Stopped, endedAt, cancellationToken);

        return new StopSimulationRunResult { Success = true };
    }
}
