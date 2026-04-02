using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driving;

namespace DotnetEngine.Application.ObjectType.Handlers;

public sealed class CreateObjectTypeSchemaCommandHandler : ICreateObjectTypeSchemaCommand
{
    private readonly IObjectTypeSchemaRepository _repository;

    public CreateObjectTypeSchemaCommandHandler(IObjectTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ObjectTypeSchemaDto> CreateAsync(CreateObjectTypeSchemaRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new ObjectTypeSchemaDto
        {
            SchemaVersion = request.SchemaVersion,
            ObjectType = request.ObjectType,
            DisplayName = request.DisplayName,
            Traits = request.Traits,
            Classifications = request.Classifications,
            OwnProperties = request.OwnProperties,
            AllowedLinks = request.AllowedLinks,
            CreatedAt = now,
            UpdatedAt = now
        };
        return await _repository.CreateAsync(dto, cancellationToken);
    }
}
