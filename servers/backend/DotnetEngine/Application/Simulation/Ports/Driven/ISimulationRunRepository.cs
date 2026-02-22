using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// SimulationRun 저장소 Port (Secondary/Driven).
/// EndAsync 호출 시 내부적으로 Status=Completed 및 EndedAt 설정.
/// </summary>
public interface ISimulationRunRepository
{
    Task<SimulationRunDto> CreateAsync(SimulationRunDto dto, CancellationToken cancellationToken = default);
    Task<SimulationRunDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SimulationRunDto>> GetRunningAsync(CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string id, SimulationRunStatus status, DateTimeOffset? endedAt, CancellationToken cancellationToken = default);
    Task UpdateTickIndexAsync(string id, int tickIndex, CancellationToken cancellationToken = default);
    Task EndAsync(string id, DateTimeOffset endedAt, CancellationToken cancellationToken = default);
}
