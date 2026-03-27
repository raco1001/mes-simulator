using System.Text.Json;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driving;
using DotnetEngine.Application.Alert.Ports.Driven;
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
    private readonly IAlertNotifier _alertNotifier;

    public AlertController(IGetAlertsQuery getAlertsQuery, IAlertNotifier alertNotifier)
    {
        _getAlertsQuery = getAlertsQuery;
        _alertNotifier = alertNotifier;
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

    /// <summary>
    /// GET /api/alerts/stream — 신규 알람 SSE 스트림.
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamAlerts(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var alert in _alertNotifier.SubscribeAsync(HttpContext.RequestAborted))
        {
            var payload = JsonSerializer.Serialize(alert);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
