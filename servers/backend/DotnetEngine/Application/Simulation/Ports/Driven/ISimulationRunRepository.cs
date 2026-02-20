using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// SimulationRun 저장소 Port (Secondary/Driven).
/// </summary>
public interface ISimulationRunRepository
{
    Task<SimulationRunDto> CreateAsync(SimulationRunDto dto, CancellationToken cancellationToken = default);
    Task<SimulationRunDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task EndAsync(string id, DateTimeOffset endedAt, CancellationToken cancellationToken = default);
}
