using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.ObjectType.Ports.Driving;

public interface IGetObjectTypeSchemaQuery
{
    Task<ObjectTypeSchemaDto?> GetByObjectTypeAsync(string objectType, CancellationToken cancellationToken = default);
}
