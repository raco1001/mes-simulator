using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.LinkType.Ports.Driving;

namespace DotnetEngine.Application.LinkType.Handlers;

public sealed class GetLinkTypeSchemasQueryHandler : IGetLinkTypeSchemasQuery
{
    private readonly ILinkTypeSchemaRepository _repository;

    public GetLinkTypeSchemasQueryHandler(ILinkTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<LinkTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }
}
