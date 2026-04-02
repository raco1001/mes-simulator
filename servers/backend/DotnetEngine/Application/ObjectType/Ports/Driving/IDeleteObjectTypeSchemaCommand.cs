namespace DotnetEngine.Application.ObjectType.Ports.Driving;

public interface IDeleteObjectTypeSchemaCommand
{
    Task<bool> DeleteAsync(string objectType, CancellationToken cancellationToken = default);
}
