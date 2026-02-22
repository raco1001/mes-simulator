using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// 시뮬레이션 실행 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase
{
    private readonly IRunSimulationCommand _runSimulationCommand;
    private readonly IStartContinuousRunCommand _startContinuousRunCommand;
    private readonly IStopSimulationRunCommand _stopSimulationRunCommand;
    private readonly IReplayRunCommand _replayRunCommand;
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEventRepository _eventRepository;

    public SimulationController(
        IRunSimulationCommand runSimulationCommand,
        IStartContinuousRunCommand startContinuousRunCommand,
        IStopSimulationRunCommand stopSimulationRunCommand,
        IReplayRunCommand replayRunCommand,
        ISimulationRunRepository simulationRunRepository,
        IEventRepository eventRepository)
    {
        _runSimulationCommand = runSimulationCommand;
        _startContinuousRunCommand = startContinuousRunCommand;
        _stopSimulationRunCommand = stopSimulationRunCommand;
        _replayRunCommand = replayRunCommand;
        _simulationRunRepository = simulationRunRepository;
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// POST /api/simulation/runs/start — 지속 시뮬레이션 시작. Run 생성(Status=Running), 전파는 호출하지 않음. runId 반환.
    /// </summary>
    [HttpPost("runs/start")]
    [ProducesResponseType(typeof(StartContinuousRunResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartContinuousRun([FromBody] RunSimulationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TriggerAssetId))
            return BadRequest(new { error = "triggerAssetId is required" });

        var result = await _startContinuousRunCommand.StartAsync(request, cancellationToken);
        if (!result.Success)
            return StatusCode(StatusCodes.Status409Conflict, result);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// POST /api/simulation/runs/{runId}/stop — 해당 Run 중단. Status=Stopped, EndedAt 설정.
    /// </summary>
    [HttpPost("runs/{runId}/stop")]
    [ProducesResponseType(typeof(StopSimulationRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopRun(string runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return NotFound();

        var result = await _stopSimulationRunCommand.StopAsync(runId, cancellationToken);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// POST /api/simulation/runs — 시뮬레이션 런 1회 실행. 트리거 에셋 + BFS 전파, runId 반환.
    /// </summary>
    [HttpPost("runs")]
    [ProducesResponseType(typeof(RunResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun([FromBody] RunSimulationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TriggerAssetId))
            return BadRequest(new { error = "triggerAssetId is required" });

        var result = await _runSimulationCommand.RunAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// GET /api/simulation/runs/{runId}/events — run 단위 이벤트 조회. Optional tickMax: events with RunTick &lt;= tickMax only (replay 상한).
    /// </summary>
    [HttpGet("runs/{runId}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunEvents(string runId, [FromQuery] int? tickMax, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return NotFound();

        var run = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return NotFound();

        var events = await _eventRepository.GetBySimulationRunIdAsync(runId, tickMax, cancellationToken);
        return Ok(events);
    }

    /// <summary>
    /// POST /api/simulation/runs/{runId}/replay — 저장된 이벤트로 상태만 재적용(Replay). Optional tickMax: replay up to that tick.
    /// </summary>
    [HttpPost("runs/{runId}/replay")]
    [ProducesResponseType(typeof(ReplayRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayRun(string runId, [FromQuery] int? tickMax, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return NotFound();

        var result = await _replayRunCommand.ReplayAsync(runId, tickMax, cancellationToken);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
