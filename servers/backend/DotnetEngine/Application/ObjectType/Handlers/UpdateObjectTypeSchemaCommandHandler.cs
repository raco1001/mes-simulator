using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driving;

namespace DotnetEngine.Application.ObjectType.Handlers;

public sealed class UpdateObjectTypeSchemaCommandHandler : IUpdateObjectTypeSchemaCommand
{
    private readonly IObjectTypeSchemaRepository _repository;

    public UpdateObjectTypeSchemaCommandHandler(IObjectTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ObjectTypeSchemaDto?> UpdateAsync(string objectType, UpdateObjectTypeSchemaRequest request, CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetByObjectTypeAsync(objectType, cancellationToken);
        if (current is null)
            return null;

        var next = current with
        {
            SchemaVersion = request.SchemaVersion ?? current.SchemaVersion,
            DisplayName = request.DisplayName ?? current.DisplayName,
            Traits = request.Traits ?? current.Traits,
            Classifications = request.Classifications ?? current.Classifications,
            OwnProperties = request.OwnProperties ?? current.OwnProperties,
            AllowedLinks = request.AllowedLinks ?? current.AllowedLinks,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return await _repository.UpdateAsync(objectType, next, cancellationToken);
    }
}
