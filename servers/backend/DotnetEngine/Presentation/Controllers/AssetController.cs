using DotnetEngine.Application.Asset.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Asset 조회/생성/수정 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/assets")]
public sealed class AssetController : ControllerBase
{
    private readonly IGetAssetsQuery _getAssetsQuery;
    private readonly ICreateAssetCommand _createAssetCommand;
    private readonly IUpdateAssetCommand _updateAssetCommand;

    public AssetController(
        IGetAssetsQuery getAssetsQuery,
        ICreateAssetCommand createAssetCommand,
        IUpdateAssetCommand updateAssetCommand)
    {
        _getAssetsQuery = getAssetsQuery;
        _createAssetCommand = createAssetCommand;
        _updateAssetCommand = updateAssetCommand;
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

    /// <summary>
    /// POST /api/assets — Asset 생성.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Application.Asset.Dto.AssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Application.Asset.Dto.CreateAssetRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest();
        }
        var dto = await _createAssetCommand.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// PUT /api/assets/{id} — 특정 asset 수정.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Application.Asset.Dto.AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] Application.Asset.Dto.UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest();
        }
        var dto = await _updateAssetCommand.UpdateAsync(id, request, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return Ok(dto);
    }
}
