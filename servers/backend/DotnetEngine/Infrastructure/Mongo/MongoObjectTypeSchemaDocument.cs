using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoObjectTypeSchemaDocument
{
    [BsonId]
    public string ObjectType { get; set; } = string.Empty;

    [BsonElement("objectType")]
    public string ObjectTypeValue { get; set; } = string.Empty;

    [BsonElement("payloadJson")]
    public BsonDocument PayloadJson { get; set; } = new();
}
