using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Alert 조회 Adapter. Kafka에서 소비한 최신 알람 목록 제공.
/// </summary>
[ApiController]
[Route("api/alerts")]
public sealed class AlertController : ControllerBase
{
    private readonly IGetAlertsQuery _getAlertsQuery;

    public AlertController(IGetAlertsQuery getAlertsQuery)
    {
        _getAlertsQuery = getAlertsQuery;
    }

    /// <summary>
    /// GET /api/alerts — 최신 알람 목록 반환 (optional limit, default 50).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var dtos = await _getAlertsQuery.GetLatestAsync(limit, cancellationToken);
        return Ok(dtos);
    }
}
