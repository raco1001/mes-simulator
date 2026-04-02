using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driving;

namespace DotnetEngine.Application.ObjectType.Handlers;

public sealed class GetObjectTypeSchemasQueryHandler : IGetObjectTypeSchemasQuery
{
    private readonly IObjectTypeSchemaRepository _repository;

    public GetObjectTypeSchemasQueryHandler(IObjectTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ObjectTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var schemas = await _repository.GetAllAsync(cancellationToken);
        var schemaMap = schemas.ToDictionary(s => s.ObjectType);

        return schemas
            .Select(s => ResolveInheritance(s, schemaMap))
            .ToList();
    }

    private static ObjectTypeSchemaDto ResolveInheritance(
        ObjectTypeSchemaDto schema,
        IReadOnlyDictionary<string, ObjectTypeSchemaDto> schemaMap)
    {
        if (schema.Extends is null)
            return schema with { ResolvedProperties = schema.OwnProperties };

        var parentProperties = schemaMap.TryGetValue(schema.Extends, out var parent)
            ? (parent.ResolvedProperties ?? parent.OwnProperties)
            : [];

        var resolved = parentProperties
            .Concat(schema.OwnProperties)
            .ToList();

        return schema with { ResolvedProperties = resolved };
    }
}
