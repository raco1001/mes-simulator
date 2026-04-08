using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.Relationship.Dto;

namespace DotnetEngine.Application.Relationship.Ports.Driven;

public interface IRelationshipPropertyMappingValidator
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> when mappings are invalid for the given assets and optional link schema.
    /// </summary>
    Task ValidateAsync(
        IReadOnlyList<PropertyMapping> mappings,
        string fromAssetId,
        string toAssetId,
        LinkTypeSchemaDto? linkSchema,
        CancellationToken cancellationToken = default);
}
