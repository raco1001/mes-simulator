using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.ObjectType.Ports.Driving;

public interface IUpdateObjectTypeSchemaCommand
{
    Task<ObjectTypeSchemaDto?> UpdateAsync(string objectType, UpdateObjectTypeSchemaRequest request, CancellationToken cancellationToken = default);
}
