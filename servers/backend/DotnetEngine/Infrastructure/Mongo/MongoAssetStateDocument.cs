using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoAssetStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;
    [BsonElement("currentTemp")]
    public double? CurrentTemp { get; set; }
    [BsonElement("currentPower")]
    public double? CurrentPower { get; set; }
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;
    [BsonElement("lastEventType")]
    public string? LastEventType { get; set; }
    [BsonElement("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
    [BsonElement("metadata")]
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
