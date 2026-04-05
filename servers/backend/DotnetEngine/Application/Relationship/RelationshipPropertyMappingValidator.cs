using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;

namespace DotnetEngine.Application.Relationship;

public sealed class RelationshipPropertyMappingValidator : IRelationshipPropertyMappingValidator
{
    private readonly IAssetRepository _assetRepository;
    private readonly IObjectTypeSchemaRepository _objectTypeSchemaRepository;

    public RelationshipPropertyMappingValidator(
        IAssetRepository assetRepository,
        IObjectTypeSchemaRepository objectTypeSchemaRepository)
    {
        _assetRepository = assetRepository;
        _objectTypeSchemaRepository = objectTypeSchemaRepository;
    }

    public async Task ValidateAsync(
        IReadOnlyList<PropertyMapping> mappings,
        string fromAssetId,
        string toAssetId,
        LinkTypeSchemaDto? linkSchema,
        CancellationToken cancellationToken = default)
    {
        if (mappings.Count == 0)
            return;

        var fromAsset = await _assetRepository.GetByIdAsync(fromAssetId, cancellationToken)
            ?? throw new ArgumentException("From asset was not found.", nameof(fromAssetId));
        var toAsset = await _assetRepository.GetByIdAsync(toAssetId, cancellationToken)
            ?? throw new ArgumentException("To asset was not found.", nameof(toAssetId));

        var fromSchema = await _objectTypeSchemaRepository.GetByObjectTypeAsync(fromAsset.Type, cancellationToken);
        var toSchema = await _objectTypeSchemaRepository.GetByObjectTypeAsync(toAsset.Type, cancellationToken);

        var fromDefs = EffectivePropertySetResolver.Resolve(fromSchema, fromAsset);
        var toDefs = EffectivePropertySetResolver.Resolve(toSchema, toAsset);

        foreach (var m in mappings)
        {
            if (string.IsNullOrWhiteSpace(m.FromProperty) || string.IsNullOrWhiteSpace(m.ToProperty))
                continue;

            var fromDef = fromDefs.FirstOrDefault(p => p.Key == m.FromProperty);
            var toDef = toDefs.FirstOrDefault(p => p.Key == m.ToProperty);

            if (fromDef is null)
                throw new ArgumentException($"Source property '{m.FromProperty}' is not defined on the from asset's schema or extraProperties.");
            if (toDef is null)
                throw new ArgumentException($"Target property '{m.ToProperty}' is not defined on the to asset's schema or extraProperties.");

            if (fromDef.DataType != DataType.Number || toDef.DataType != DataType.Number)
                throw new ArgumentException($"Property mapping requires Number data types for both ends: '{m.FromProperty}' → '{m.ToProperty}'.");

            var unitFrom = string.IsNullOrWhiteSpace(m.FromUnit) ? fromDef.Unit : m.FromUnit;
            var unitTo = string.IsNullOrWhiteSpace(m.ToUnit) ? toDef.Unit : m.ToUnit;

            if (!SimpleUnitConverter.AreCompatible(unitFrom, unitTo))
                throw new ArgumentException(
                    $"Incompatible units for mapping '{m.FromProperty}' → '{m.ToProperty}': '{unitFrom ?? "(none)"}' vs '{unitTo ?? "(none)"}'.");
        }

        if (linkSchema is { AllowedPropertyMappingPairs.Count: > 0 })
        {
            foreach (var m in mappings)
            {
                if (string.IsNullOrWhiteSpace(m.FromProperty) || string.IsNullOrWhiteSpace(m.ToProperty))
                    continue;

                var ok = linkSchema.AllowedPropertyMappingPairs.Any(p =>
                    string.Equals(p.FromPropertyKey, m.FromProperty, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.ToPropertyKey, m.ToProperty, StringComparison.OrdinalIgnoreCase));
                if (!ok)
                    throw new ArgumentException(
                        $"Mapping '{m.FromProperty}' → '{m.ToProperty}' is not allowed for link type '{linkSchema.LinkType}'.");
            }
        }
    }
}
