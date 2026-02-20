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
    /// POST /api/simulation/run — 시뮬레이션 1회 실행. 에셋·관계 기반 최소 시뮬레이션 후 결과 반환.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(RunResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await _runSimulationCommand.RunAsync(cancellationToken);
        return Ok(result);
    }
}
