using DotnetEngine.Application.LinkType.Dto;

namespace DotnetEngine.Application.LinkType.Ports.Driving;

public interface IGetLinkTypeSchemasQuery
{
    Task<IReadOnlyList<LinkTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
