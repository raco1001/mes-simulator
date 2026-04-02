using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.LinkType.Ports.Driving;

namespace DotnetEngine.Application.LinkType.Handlers;

public sealed class CreateLinkTypeSchemaCommandHandler : ICreateLinkTypeSchemaCommand
{
    private readonly ILinkTypeSchemaRepository _repository;

    public CreateLinkTypeSchemaCommandHandler(ILinkTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<LinkTypeSchemaDto> CreateAsync(CreateLinkTypeSchemaRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new LinkTypeSchemaDto
        {
            SchemaVersion = request.SchemaVersion,
            LinkType = request.LinkType,
            DisplayName = request.DisplayName,
            Direction = request.Direction,
            Temporality = request.Temporality,
            FromConstraint = request.FromConstraint,
            ToConstraint = request.ToConstraint,
            Properties = request.Properties,
            CreatedAt = now,
            UpdatedAt = now
        };
        return await _repository.CreateAsync(dto, cancellationToken);
    }
}
