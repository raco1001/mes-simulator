using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoRelationshipDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("fromAssetId")]
    public string FromAssetId { get; set; } = string.Empty;

    [BsonElement("toAssetId")]
    public string ToAssetId { get; set; } = string.Empty;

    [BsonElement("relationshipType")]
    public string RelationshipType { get; set; } = string.Empty;

    [BsonElement("properties")]
    public BsonDocument Properties { get; set; } = new BsonDocument();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
