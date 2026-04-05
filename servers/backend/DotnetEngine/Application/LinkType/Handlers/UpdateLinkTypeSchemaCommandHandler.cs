using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.LinkType.Ports.Driving;

namespace DotnetEngine.Application.LinkType.Handlers;

public sealed class UpdateLinkTypeSchemaCommandHandler : IUpdateLinkTypeSchemaCommand
{
    private readonly ILinkTypeSchemaRepository _repository;

    public UpdateLinkTypeSchemaCommandHandler(ILinkTypeSchemaRepository repository)
    {
        _repository = repository;
    }

    public async Task<LinkTypeSchemaDto?> UpdateAsync(string linkType, UpdateLinkTypeSchemaRequest request, CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetByLinkTypeAsync(linkType, cancellationToken);
        if (current is null)
            return null;

        var next = current with
        {
            SchemaVersion = request.SchemaVersion ?? current.SchemaVersion,
            DisplayName = request.DisplayName ?? current.DisplayName,
            Direction = request.Direction ?? current.Direction,
            Temporality = request.Temporality ?? current.Temporality,
            FromConstraint = request.FromConstraint ?? current.FromConstraint,
            ToConstraint = request.ToConstraint ?? current.ToConstraint,
            Properties = request.Properties ?? current.Properties,
            DefaultPropertyMappings = request.DefaultPropertyMappings ?? current.DefaultPropertyMappings,
            AllowedPropertyMappingPairs = request.AllowedPropertyMappingPairs ?? current.AllowedPropertyMappingPairs,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return await _repository.UpdateAsync(linkType, next, cancellationToken);
    }
}
