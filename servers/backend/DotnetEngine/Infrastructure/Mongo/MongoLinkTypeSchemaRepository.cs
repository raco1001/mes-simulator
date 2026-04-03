using DotnetEngine.Application.LinkType.Dto;
using DotnetEngine.Application.LinkType.Ports.Driven;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoLinkTypeSchemaRepository : ILinkTypeSchemaRepository
{
    private readonly IMongoCollection<MongoLinkTypeSchemaDocument> _collection;

    public MongoLinkTypeSchemaRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoLinkTypeSchemaDocument>("link_type_schemas");
    }

    public async Task<IReadOnlyList<LinkTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(FilterDefinition<MongoLinkTypeSchemaDocument>.Empty, cancellationToken: cancellationToken);
        var docs = await cursor.ToListAsync(cancellationToken);
        return docs
            .Select(ToDto)
            .Where(x => x is not null)
            .Cast<LinkTypeSchemaDto>()
            .ToList();
    }

    public async Task<LinkTypeSchemaDto?> GetByLinkTypeAsync(string linkType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoLinkTypeSchemaDocument>.Filter.Eq(x => x.LinkTypeValue, linkType);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : ToDto(doc);
    }

    public async Task<LinkTypeSchemaDto> CreateAsync(LinkTypeSchemaDto dto, CancellationToken cancellationToken = default)
    {
        var doc = ToDocument(dto);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return dto;
    }

    public async Task<LinkTypeSchemaDto?> UpdateAsync(string linkType, LinkTypeSchemaDto dto, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoLinkTypeSchemaDocument>.Filter.Eq(x => x.LinkTypeValue, linkType);
        var doc = ToDocument(dto);
        var result = await _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        return result.MatchedCount == 0 ? null : dto;
    }

    private static MongoLinkTypeSchemaDocument ToDocument(LinkTypeSchemaDto dto)
    {
        return new MongoLinkTypeSchemaDocument
        {
            LinkType = dto.LinkType,
            LinkTypeValue = dto.LinkType,
            PayloadJson = dto.ToBsonDocument()
        };
    }

    private static LinkTypeSchemaDto? ToDto(MongoLinkTypeSchemaDocument doc)
    {
        try
        {
            return BsonSerializer.Deserialize<LinkTypeSchemaDto>(doc.PayloadJson);
        }
        catch
        {
            return null;
        }
    }
}
