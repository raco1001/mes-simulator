using DotnetEngine.Application.LinkType.Dto;

namespace DotnetEngine.Application.LinkType.Ports.Driving;

public interface IGetLinkTypeSchemaQuery
{
    Task<LinkTypeSchemaDto?> GetByLinkTypeAsync(string linkType, CancellationToken cancellationToken = default);
}
