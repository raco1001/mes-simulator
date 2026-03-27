using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoAlertDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("severity")]
    public string Severity { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("runId")]
    public string? RunId { get; set; }

    [BsonElement("metric")]
    public string? Metric { get; set; }

    [BsonElement("current")]
    public double? Current { get; set; }

    [BsonElement("threshold")]
    public double? Threshold { get; set; }

    [BsonElement("code")]
    public string? Code { get; set; }

    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new();
}
