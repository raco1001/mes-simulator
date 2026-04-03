using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoLinkTypeSchemaDocument
{
    [BsonId]
    public string LinkType { get; set; } = string.Empty;

    [BsonElement("linkType")]
    public string LinkTypeValue { get; set; } = string.Empty;

    [BsonElement("payloadJson")]
    public BsonDocument PayloadJson { get; set; } = new();
}
