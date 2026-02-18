using DotnetEngine.Application.Asset.Ports;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Asset 상태 조회 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/states")]
public sealed class StateController : ControllerBase
{
    private readonly IGetStatesQuery _getStatesQuery;

    public StateController(IGetStatesQuery getStatesQuery)
    {
        _getStatesQuery = getStatesQuery;
    }

    /// <summary>
    /// GET /api/states — 모든 asset의 현재 상태 반환.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Application.Asset.Dto.StateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var dtos = await _getStatesQuery.GetAllAsync(cancellationToken);
        return Ok(dtos);
    }

    /// <summary>
    /// GET /api/states/{assetId} — 특정 asset의 현재 상태 반환.
    /// </summary>
    [HttpGet("{assetId}")]
    [ProducesResponseType(typeof(Application.Asset.Dto.StateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAssetId(string assetId, CancellationToken cancellationToken)
    {
        var dto = await _getStatesQuery.GetByAssetIdAsync(assetId, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return Ok(dto);
    }
}
