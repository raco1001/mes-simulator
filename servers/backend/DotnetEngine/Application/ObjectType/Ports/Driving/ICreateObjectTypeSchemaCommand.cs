using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.ObjectType.Ports.Driving;

public interface ICreateObjectTypeSchemaCommand
{
    Task<ObjectTypeSchemaDto> CreateAsync(CreateObjectTypeSchemaRequest request, CancellationToken cancellationToken = default);
}
