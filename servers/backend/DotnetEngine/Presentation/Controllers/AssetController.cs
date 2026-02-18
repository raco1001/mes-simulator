using DotnetEngine.Application.Asset.Ports;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Asset 조회 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/assets")]
public sealed class AssetController : ControllerBase
{
    private readonly IGetAssetsQuery _getAssetsQuery;

    public AssetController(IGetAssetsQuery getAssetsQuery)
    {
        _getAssetsQuery = getAssetsQuery;
    }

    /// <summary>
    /// GET /api/assets — 모든 asset 목록 반환.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Application.Asset.Dto.AssetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var dtos = await _getAssetsQuery.GetAllAsync(cancellationToken);
        return Ok(dtos);
    }

    /// <summary>
    /// GET /api/assets/{id} — 특정 asset 정보 반환.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Application.Asset.Dto.AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var dto = await _getAssetsQuery.GetByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return Ok(dto);
    }
}
