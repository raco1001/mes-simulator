using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

[ApiController]
[Route("api/object-type-schemas")]
public sealed class ObjectTypeSchemaController : ControllerBase
{
    private readonly IGetObjectTypeSchemasQuery _getAllQuery;
    private readonly IGetObjectTypeSchemaQuery _getOneQuery;
    private readonly ICreateObjectTypeSchemaCommand _createCommand;
    private readonly IUpdateObjectTypeSchemaCommand _updateCommand;
    private readonly IDeleteObjectTypeSchemaCommand _deleteCommand;

    public ObjectTypeSchemaController(
        IGetObjectTypeSchemasQuery getAllQuery,
        IGetObjectTypeSchemaQuery getOneQuery,
        ICreateObjectTypeSchemaCommand createCommand,
        IUpdateObjectTypeSchemaCommand updateCommand,
        IDeleteObjectTypeSchemaCommand deleteCommand)
    {
        _getAllQuery = getAllQuery;
        _getOneQuery = getOneQuery;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
        _deleteCommand = deleteCommand;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ObjectTypeSchemaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _getAllQuery.GetAllAsync(cancellationToken));
    }

    [HttpGet("{objectType}")]
    [ProducesResponseType(typeof(ObjectTypeSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByObjectType(string objectType, CancellationToken cancellationToken)
    {
        var dto = await _getOneQuery.GetByObjectTypeAsync(objectType, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectTypeSchemaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateObjectTypeSchemaRequest request, CancellationToken cancellationToken)
    {
        var dto = await _createCommand.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByObjectType), new { objectType = dto.ObjectType }, dto);
    }

    [HttpPut("{objectType}")]
    [ProducesResponseType(typeof(ObjectTypeSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string objectType, [FromBody] UpdateObjectTypeSchemaRequest request, CancellationToken cancellationToken)
    {
        var dto = await _updateCommand.UpdateAsync(objectType, request, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{objectType}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string objectType, CancellationToken cancellationToken)
    {
        var deleted = await _deleteCommand.DeleteAsync(objectType, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
