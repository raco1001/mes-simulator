using DotnetEngine.Application.Health.Ports;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Health 상태 조회 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IGetHealthQuery _getHealthQuery;

    public HealthController(IGetHealthQuery getHealthQuery)
    {
        _getHealthQuery = getHealthQuery;
    }

    /// <summary>
    /// GET /api/health — 애플리케이션 Health 상태 반환.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Health.Dto.HealthStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dto = await _getHealthQuery.GetAsync(cancellationToken);
        return Ok(dto);
    }
}
