namespace DotnetEngine.Domain.Simulation.ValueObjects;

/// <summary>
/// 시뮬레이션 런 생명주기 상태.
/// </summary>
public enum SimulationRunStatus
{
    Pending,
    Running,
    Stopped,
    Completed
}
