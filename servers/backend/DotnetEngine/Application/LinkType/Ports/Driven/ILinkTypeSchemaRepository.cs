using DotnetEngine.Application.LinkType.Dto;

namespace DotnetEngine.Application.LinkType.Ports.Driven;

public interface ILinkTypeSchemaRepository
{
    Task<IReadOnlyList<LinkTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LinkTypeSchemaDto?> GetByLinkTypeAsync(string linkType, CancellationToken cancellationToken = default);
    Task<LinkTypeSchemaDto> CreateAsync(LinkTypeSchemaDto dto, CancellationToken cancellationToken = default);
    Task<LinkTypeSchemaDto?> UpdateAsync(string linkType, LinkTypeSchemaDto dto, CancellationToken cancellationToken = default);
}
