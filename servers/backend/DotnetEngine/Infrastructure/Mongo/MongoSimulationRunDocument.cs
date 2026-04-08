using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoSimulationRunDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Pending";

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    /// <summary>Legacy single seed; prefer <see cref="TriggerAssetIds"/> when present.</summary>
    [BsonElement("triggerAssetId")]
    public string TriggerAssetId { get; set; } = string.Empty;

    [BsonElement("triggerAssetIds")]
    public List<string> TriggerAssetIds { get; set; } = new();

    [BsonElement("trigger")]
    public BsonDocument Trigger { get; set; } = new BsonDocument();

    [BsonElement("maxDepth")]
    public int MaxDepth { get; set; }

    [BsonElement("tickIndex")]
    public int TickIndex { get; set; }

    [BsonElement("engineTickIntervalMs")]
    public int EngineTickIntervalMs { get; set; } = 1000;

    [BsonElement("initialSnapshot")]
    public BsonDocument InitialSnapshot { get; set; } = new BsonDocument();

    [BsonElement("overrides")]
    public List<BsonDocument> Overrides { get; set; } = new();
}
