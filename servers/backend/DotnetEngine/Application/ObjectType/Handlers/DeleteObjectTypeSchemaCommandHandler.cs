using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.ObjectType.Ports.Driving;

namespace DotnetEngine.Application.ObjectType.Handlers;

public sealed class DeleteObjectTypeSchemaCommandHandler : IDeleteObjectTypeSchemaCommand
{
    private readonly IObjectTypeSchemaRepository _repository;

    public DeleteObjectTypeSchemaCommandHandler(IObjectTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> DeleteAsync(string objectType, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(objectType, cancellationToken);
    }
}
