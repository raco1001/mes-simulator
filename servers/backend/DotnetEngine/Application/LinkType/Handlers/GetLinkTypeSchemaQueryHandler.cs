using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.LinkType.Ports.Driving;

namespace DotnetEngine.Application.LinkType.Handlers;

public sealed class GetLinkTypeSchemaQueryHandler : IGetLinkTypeSchemaQuery
{
    private readonly ILinkTypeSchemaRepository _repository;

    public GetLinkTypeSchemaQueryHandler(ILinkTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public Task<LinkTypeSchemaDto?> GetByLinkTypeAsync(string linkType, CancellationToken cancellationToken = default)
    {
        return _repository.GetByLinkTypeAsync(linkType, cancellationToken);
    }
}
