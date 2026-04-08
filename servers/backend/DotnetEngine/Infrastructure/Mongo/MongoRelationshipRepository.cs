using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// MongoDB Relationship Repository 구현 (Adapter - Secondary/Driven).
/// </summary>
public sealed class MongoRelationshipRepository : IRelationshipRepository
{
    private readonly IMongoCollection<MongoRelationshipDocument> _collection;

    public MongoRelationshipRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoRelationshipDocument>("relationships");
    }

    public async Task<IReadOnlyList<RelationshipDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(FilterDefinition<MongoRelationshipDocument>.Empty, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);
        return documents.Count == 0 ? new List<RelationshipDto>() : documents.Select(ToDto).ToList();
    }

    public async Task<RelationshipDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoRelationshipDocument>.Filter.Eq(d => d.Id, id);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDto(document);
    }

    public async Task<IReadOnlyList<RelationshipDto>> GetOutgoingAsync(string fromAssetId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoRelationshipDocument>.Filter.Eq(d => d.FromAssetId, fromAssetId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);
        return documents.Count == 0 ? new List<RelationshipDto>() : documents.Select(ToDto).ToList();
    }

    public async Task<RelationshipDto> AddAsync(RelationshipDto dto, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(dto);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToDto(document);
    }

    public async Task<RelationshipDto?> UpdateAsync(string id, RelationshipDto dto, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoRelationshipDocument>.Filter.Eq(d => d.Id, id);
        var document = ToDocument(dto);
        var result = await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        return result.ModifiedCount > 0 ? ToDto(document) : null;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoRelationshipDocument>.Filter.Eq(d => d.Id, id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ExistsForAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoRelationshipDocument>.Filter.Or(
            Builders<MongoRelationshipDocument>.Filter.Eq(d => d.FromAssetId, assetId),
            Builders<MongoRelationshipDocument>.Filter.Eq(d => d.ToAssetId, assetId));
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    private static RelationshipDto ToDto(MongoRelationshipDocument doc)
    {
        return new RelationshipDto
        {
            Id = doc.Id,
            FromAssetId = doc.FromAssetId,
            ToAssetId = doc.ToAssetId,
            RelationshipType = doc.RelationshipType,
            Properties = MetadataBsonConverter.ToDictionary(doc.Properties),
            Mappings = ToMappingDtos(doc.Mappings),
            CreatedAt = ToDateTimeOffset(doc.CreatedAt),
            UpdatedAt = ToDateTimeOffset(doc.UpdatedAt)
        };
    }

    private static IReadOnlyList<PropertyMapping> ToMappingDtos(List<MongoPropertyMapping>? list)
    {
        if (list is null || list.Count == 0)
            return Array.Empty<PropertyMapping>();
        var result = new PropertyMapping[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            var m = list[i];
            result[i] = new PropertyMapping(
                m.FromProperty,
                m.ToProperty,
                string.IsNullOrWhiteSpace(m.TransformRule) ? "value" : m.TransformRule,
                m.FromUnit,
                m.ToUnit);
        }
        return result;
    }

    private static MongoRelationshipDocument ToDocument(RelationshipDto dto)
    {
        return new MongoRelationshipDocument
        {
            Id = dto.Id,
            FromAssetId = dto.FromAssetId,
            ToAssetId = dto.ToAssetId,
            RelationshipType = dto.RelationshipType,
            Properties = MetadataBsonConverter.ToBsonDocument(dto.Properties),
            Mappings = ToMongoMappings(dto.Mappings),
            CreatedAt = dto.CreatedAt.UtcDateTime,
            UpdatedAt = dto.UpdatedAt.UtcDateTime
        };
    }

    private static List<MongoPropertyMapping>? ToMongoMappings(IReadOnlyList<PropertyMapping> mappings)
    {
        if (mappings.Count == 0)
            return null;
        return mappings.Select(m => new MongoPropertyMapping
        {
            FromProperty = m.FromProperty,
            ToProperty = m.ToProperty,
            TransformRule = string.IsNullOrWhiteSpace(m.TransformRule) ? "value" : m.TransformRule,
            FromUnit = m.FromUnit,
            ToUnit = m.ToUnit
        }).ToList();
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
