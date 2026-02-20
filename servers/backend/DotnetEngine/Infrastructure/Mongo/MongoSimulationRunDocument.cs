using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoSimulationRunDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("triggerAssetId")]
    public string TriggerAssetId { get; set; } = string.Empty;

    [BsonElement("trigger")]
    public BsonDocument Trigger { get; set; } = new BsonDocument();

    [BsonElement("maxDepth")]
    public int MaxDepth { get; set; }
}
