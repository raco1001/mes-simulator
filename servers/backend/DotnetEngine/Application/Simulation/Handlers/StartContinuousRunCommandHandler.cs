using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Ports.Driving;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 지속 시뮬레이션 시작 Use Case. Run 생성(Status=Running), 전파는 호출하지 않음.
/// 동시에 Running인 Run이 1개를 넘지 않도록 start 시 검사.
/// </summary>
public sealed class StartContinuousRunCommandHandler : IStartContinuousRunCommand
{
    private readonly ISimulationRunRepository _simulationRunRepository;

    public StartContinuousRunCommandHandler(ISimulationRunRepository simulationRunRepository)
    {
        _simulationRunRepository = simulationRunRepository;
    }

    public async Task<StartContinuousRunResult> StartAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var running = await _simulationRunRepository.GetRunningAsync(cancellationToken);
        if (running.Count >= 1)
            return new StartContinuousRunResult
            {
                Success = false,
                RunId = "",
                Message = "Another simulation run is already running. Stop it before starting a new one.",
            };

        var runId = Guid.NewGuid().ToString("N");
        var maxDepth = request.MaxDepth <= 0 ? 3 : request.MaxDepth;
        var startedAt = DateTimeOffset.UtcNow;
        var trigger = StatePatchToDictionary(request.Patch);

        var runDto = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Running,
            StartedAt = startedAt,
            EndedAt = null,
            TriggerAssetId = request.TriggerAssetId,
            Trigger = trigger,
            MaxDepth = maxDepth,
            TickIndex = 0,
        };
        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);

        return new StartContinuousRunResult
        {
            Success = true,
            RunId = runId,
        };
    }

    private static IReadOnlyDictionary<string, object> StatePatchToDictionary(StatePatchDto? patch)
    {
        if (patch == null)
            return new Dictionary<string, object>();

        var d = new Dictionary<string, object>();
        if (patch.CurrentTemp.HasValue)
            d["currentTemp"] = patch.CurrentTemp.Value;
        if (patch.CurrentPower.HasValue)
            d["currentPower"] = patch.CurrentPower.Value;
        if (patch.Status != null)
            d["status"] = patch.Status;
        if (patch.LastEventType != null)
            d["lastEventType"] = patch.LastEventType;
        return d;
    }
}
