using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 지속 시뮬레이션 시작 Command Port. Run 생성(Status=Running), 전파는 호출하지 않음.
/// </summary>
public interface IStartContinuousRunCommand
{
    Task<StartContinuousRunResult> StartAsync(RunSimulationRequest request, CancellationToken cancellationToken = default);
}
