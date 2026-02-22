using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// 이벤트 로그 저장소 Port (Secondary/Driven). Append-only.
/// </summary>
public interface IEventRepository
{
    Task AppendAsync(EventDto dto, CancellationToken cancellationToken = default);
    /// <summary>Events for a run, optionally up to tickMax (inclusive). Sorted by RunTick then OccurredAt.</summary>
    Task<IReadOnlyList<EventDto>> GetBySimulationRunIdAsync(string simulationRunId, int? tickMax = null, CancellationToken cancellationToken = default);
}
