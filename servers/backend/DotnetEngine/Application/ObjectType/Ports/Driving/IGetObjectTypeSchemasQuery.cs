using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.ObjectType.Ports.Driving;

public interface IGetObjectTypeSchemasQuery
{
    Task<IReadOnlyList<ObjectTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
