using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

[ApiController]
[Route("api/link-type-schemas")]
public sealed class LinkTypeSchemaController : ControllerBase
{
    private readonly IGetLinkTypeSchemasQuery _getAllQuery;
    private readonly IGetLinkTypeSchemaQuery _getOneQuery;
    private readonly ICreateLinkTypeSchemaCommand _createCommand;
    private readonly IUpdateLinkTypeSchemaCommand _updateCommand;

    public LinkTypeSchemaController(
        IGetLinkTypeSchemasQuery getAllQuery,
        IGetLinkTypeSchemaQuery getOneQuery,
        ICreateLinkTypeSchemaCommand createCommand,
        IUpdateLinkTypeSchemaCommand updateCommand)
    {
        _getAllQuery = getAllQuery;
        _getOneQuery = getOneQuery;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<LinkTypeSchemaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _getAllQuery.GetAllAsync(cancellationToken));
    }

    [HttpGet("{linkType}")]
    [ProducesResponseType(typeof(LinkTypeSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLinkType(string linkType, CancellationToken cancellationToken)
    {
        var dto = await _getOneQuery.GetByLinkTypeAsync(linkType, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LinkTypeSchemaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLinkTypeSchemaRequest request, CancellationToken cancellationToken)
    {
        var dto = await _createCommand.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByLinkType), new { linkType = dto.LinkType }, dto);
    }

    [HttpPut("{linkType}")]
    [ProducesResponseType(typeof(LinkTypeSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string linkType, [FromBody] UpdateLinkTypeSchemaRequest request, CancellationToken cancellationToken)
    {
        var dto = await _updateCommand.UpdateAsync(linkType, request, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}
