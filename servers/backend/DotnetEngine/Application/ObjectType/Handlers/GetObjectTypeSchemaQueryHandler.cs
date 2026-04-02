using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driving;

namespace DotnetEngine.Application.ObjectType.Handlers;

public sealed class GetObjectTypeSchemaQueryHandler : IGetObjectTypeSchemaQuery
{
    private readonly IObjectTypeSchemaRepository _repository;

    public GetObjectTypeSchemaQueryHandler(IObjectTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ObjectTypeSchemaDto?> GetByObjectTypeAsync(string objectType, CancellationToken cancellationToken = default)
    {
        var schema = await _repository.GetByObjectTypeAsync(objectType, cancellationToken);
        if (schema is null)
            return null;
        return await ResolveInheritanceAsync(schema, cancellationToken);
    }

    /// <summary>
    /// If the schema has an Extends reference, fetches the parent schema and merges
    /// parent.OwnProperties before child.OwnProperties into ResolvedProperties.
    /// Single-level inheritance only; deeper chains recurse via the parent's own resolve call.
    /// </summary>
    private async Task<ObjectTypeSchemaDto> ResolveInheritanceAsync(
        ObjectTypeSchemaDto schema,
        CancellationToken cancellationToken)
    {
        if (schema.Extends is null)
            return schema with { ResolvedProperties = schema.OwnProperties };

        var parent = await _repository.GetByObjectTypeAsync(schema.Extends, cancellationToken);
        var parentProperties = parent is not null
            ? (parent.ResolvedProperties ?? parent.OwnProperties)
            : [];

        var resolved = parentProperties
            .Concat(schema.OwnProperties)
            .ToList();

        return schema with { ResolvedProperties = resolved };
    }
}
