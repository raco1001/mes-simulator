using DotnetEngine.Application.LinkType.Dto;

namespace DotnetEngine.Application.LinkType.Ports.Driving;

public interface IUpdateLinkTypeSchemaCommand
{
    Task<LinkTypeSchemaDto?> UpdateAsync(string linkType, UpdateLinkTypeSchemaRequest request, CancellationToken cancellationToken = default);
}
