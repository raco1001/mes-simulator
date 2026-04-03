using DotnetEngine.Application.LinkType.Dto;

namespace DotnetEngine.Application.LinkType.Ports.Driving;

public interface ICreateLinkTypeSchemaCommand
{
    Task<LinkTypeSchemaDto> CreateAsync(CreateLinkTypeSchemaRequest request, CancellationToken cancellationToken = default);
}
