using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoAssetDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("connections")]
    public IReadOnlyList<string> Connections { get; set; } = [];

    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new BsonDocument();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}