using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoAssetStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;
    [BsonElement("properties")]
    public BsonDocument Properties { get; set; } = new BsonDocument();
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;
    [BsonElement("lastEventType")]
    public string? LastEventType { get; set; }
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new BsonDocument();
}
