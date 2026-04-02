using DotnetEngine.Application.ObjectType.Dto;

namespace DotnetEngine.Application.ObjectType.Ports.Driven;

public interface IObjectTypeSchemaRepository
{
    Task<IReadOnlyList<ObjectTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ObjectTypeSchemaDto?> GetByObjectTypeAsync(string objectType, CancellationToken cancellationToken = default);
    Task<ObjectTypeSchemaDto> CreateAsync(ObjectTypeSchemaDto dto, CancellationToken cancellationToken = default);
    Task<ObjectTypeSchemaDto?> UpdateAsync(string objectType, ObjectTypeSchemaDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string objectType, CancellationToken cancellationToken = default);
}
