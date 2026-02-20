using DotnetEngine.Application.Simulation.Dto;
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

    public SimulationController(IRunSimulationCommand runSimulationCommand)
    {
        _runSimulationCommand = runSimulationCommand;
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
}
