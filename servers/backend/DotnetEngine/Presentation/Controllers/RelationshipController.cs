using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>
/// Relationship 조회/생성/수정/삭제 Adapter (Hexagonal - primary/driving).
/// </summary>
[ApiController]
[Route("api/relationships")]
public sealed class RelationshipController : ControllerBase
{
    private readonly IGetRelationshipsQuery _getRelationshipsQuery;
    private readonly ICreateRelationshipCommand _createRelationshipCommand;
    private readonly IUpdateRelationshipCommand _updateRelationshipCommand;
    private readonly IDeleteRelationshipCommand _deleteRelationshipCommand;

    public RelationshipController(
        IGetRelationshipsQuery getRelationshipsQuery,
        ICreateRelationshipCommand createRelationshipCommand,
        IUpdateRelationshipCommand updateRelationshipCommand,
        IDeleteRelationshipCommand deleteRelationshipCommand)
    {
        _getRelationshipsQuery = getRelationshipsQuery;
        _createRelationshipCommand = createRelationshipCommand;
        _updateRelationshipCommand = updateRelationshipCommand;
        _deleteRelationshipCommand = deleteRelationshipCommand;
    }

    /// <summary>
    /// GET /api/relationships — 모든 relationship 목록 반환.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RelationshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var dtos = await _getRelationshipsQuery.GetAllAsync(cancellationToken);
        return Ok(dtos);
    }

    /// <summary>
    /// GET /api/relationships/{id} — 특정 relationship 반환.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RelationshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var dto = await _getRelationshipsQuery.GetByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return Ok(dto);
    }

    /// <summary>
    /// POST /api/relationships — Relationship 생성.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RelationshipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRelationshipRequest request, CancellationToken cancellationToken)
    {
        if (request == null
            || string.IsNullOrWhiteSpace(request.FromAssetId)
            || string.IsNullOrWhiteSpace(request.ToAssetId)
            || string.IsNullOrWhiteSpace(request.RelationshipType))
        {
            return BadRequest();
        }
        var dto = await _createRelationshipCommand.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// PUT /api/relationships/{id} — 특정 relationship 수정.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RelationshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRelationshipRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest();
        }
        var dto = await _updateRelationshipCommand.UpdateAsync(id, request, cancellationToken);
        if (dto == null)
        {
            return NotFound();
        }
        return Ok(dto);
    }

    /// <summary>
    /// DELETE /api/relationships/{id} — 특정 relationship 삭제.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _deleteRelationshipCommand.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
