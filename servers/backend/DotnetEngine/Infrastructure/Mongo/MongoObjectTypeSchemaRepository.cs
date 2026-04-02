using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoObjectTypeSchemaRepository : IObjectTypeSchemaRepository
{
    private readonly IMongoCollection<MongoObjectTypeSchemaDocument> _collection;

    public MongoObjectTypeSchemaRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoObjectTypeSchemaDocument>("object_type_schemas");
    }

    public async Task<IReadOnlyList<ObjectTypeSchemaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _collection.FindAsync(FilterDefinition<MongoObjectTypeSchemaDocument>.Empty, cancellationToken: cancellationToken);
        var docs = await cursor.ToListAsync(cancellationToken);
        return docs
            .Select(ToDto)
            .Where(x => x is not null)
            .Cast<ObjectTypeSchemaDto>()
            .ToList();
    }

    public async Task<ObjectTypeSchemaDto?> GetByObjectTypeAsync(string objectType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoObjectTypeSchemaDocument>.Filter.Eq(x => x.ObjectTypeValue, objectType);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc is null ? null : ToDto(doc);
    }

    public async Task<ObjectTypeSchemaDto> CreateAsync(ObjectTypeSchemaDto dto, CancellationToken cancellationToken = default)
    {
        var doc = ToDocument(dto);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return dto;
    }

    public async Task<ObjectTypeSchemaDto?> UpdateAsync(string objectType, ObjectTypeSchemaDto dto, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoObjectTypeSchemaDocument>.Filter.Eq(x => x.ObjectTypeValue, objectType);
        var doc = ToDocument(dto);
        var result = await _collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        return result.MatchedCount == 0 ? null : dto;
    }

    public async Task<bool> DeleteAsync(string objectType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoObjectTypeSchemaDocument>.Filter.Eq(x => x.ObjectTypeValue, objectType);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    private static MongoObjectTypeSchemaDocument ToDocument(ObjectTypeSchemaDto dto)
    {
        return new MongoObjectTypeSchemaDocument
        {
            ObjectType = dto.ObjectType,
            ObjectTypeValue = dto.ObjectType,
            PayloadJson = dto.ToBsonDocument()
        };
    }

    private static ObjectTypeSchemaDto? ToDto(MongoObjectTypeSchemaDocument doc)
    {
        try
        {
            return BsonSerializer.Deserialize<ObjectTypeSchemaDto>(doc.PayloadJson);
        }
        catch
        {
            return null;
        }
    }
}
