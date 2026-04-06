using System.Text.Json;
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
    private readonly IWhatIfSimulationQuery _whatIfSimulationQuery;
    private readonly IStartContinuousRunCommand _startContinuousRunCommand;
    private readonly IStopSimulationRunCommand _stopSimulationRunCommand;
    private readonly IReplayRunCommand _replayRunCommand;
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ISimulationNotifier _simulationNotifier;

    public SimulationController(
        IRunSimulationCommand runSimulationCommand,
        IWhatIfSimulationQuery whatIfSimulationQuery,
        IStartContinuousRunCommand startContinuousRunCommand,
        IStopSimulationRunCommand stopSimulationRunCommand,
        IReplayRunCommand replayRunCommand,
        ISimulationRunRepository simulationRunRepository,
        IEventRepository eventRepository,
        ISimulationNotifier simulationNotifier)
    {
        _runSimulationCommand = runSimulationCommand;
        _whatIfSimulationQuery = whatIfSimulationQuery;
        _startContinuousRunCommand = startContinuousRunCommand;
        _stopSimulationRunCommand = stopSimulationRunCommand;
        _replayRunCommand = replayRunCommand;
        _simulationRunRepository = simulationRunRepository;
        _eventRepository = eventRepository;
        _simulationNotifier = simulationNotifier;
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
    /// GET /api/simulation/running — Status=Running 인 런 전부 (UI 복구·중지용).
    /// </summary>
    [HttpGet("running")]
    [ProducesResponseType(typeof(IReadOnlyList<SimulationRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRunningRuns(CancellationToken cancellationToken)
    {
        var list = await _simulationRunRepository.GetRunningAsync(cancellationToken);
        return Ok(list);
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

    [HttpPost("what-if")]
    [ProducesResponseType(typeof(WhatIfResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> WhatIf([FromBody] RunSimulationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TriggerAssetId))
            return BadRequest(new { error = "triggerAssetId is required" });

        var result = await _whatIfSimulationQuery.RunAsync(request, cancellationToken);
        return Ok(result);
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


    /// <summary>
    /// GET /api/simulation/runs/{runId} — Run 메타·초기 스냅샷·override 이력.
    /// </summary>
    [HttpGet("runs/{runId}")]
    [ProducesResponseType(typeof(SimulationRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(string runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return NotFound();
        var run = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return NotFound();
        return Ok(run);
    }

    /// <summary>
    /// POST /api/simulation/runs/{runId}/overrides — override 이력 append (재현용).
    /// </summary>
    [HttpPost("runs/{runId}/overrides")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AppendOverride(string runId, [FromBody] AppendSimulationOverrideRequestBody body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId) || body == null)
            return NotFound();
        var run = await _simulationRunRepository.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return NotFound();

        var entry = new SimulationOverrideEntryDto
        {
            AssetId = body.AssetId,
            PropertyKey = body.PropertyKey,
            Value = JsonElementToObject(body.Value),
            FromTick = body.FromTick,
            ToTick = body.ToTick,
        };
        await _simulationRunRepository.AppendOverrideAsync(runId, entry, cancellationToken);
        return NoContent();
    }

    private static object JsonElementToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => "",
        JsonValueKind.Undefined => "",
        _ => el.GetRawText(),
    };

    /// <summary>
    /// GET /api/simulation/stream — 실시간 시뮬레이션 상태 SSE 스트림.
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamSimulation(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await foreach (var tickEvent in _simulationNotifier.SubscribeAsync(HttpContext.RequestAborted))
        {
            var payload = JsonSerializer.Serialize(tickEvent, jsonOptions);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
