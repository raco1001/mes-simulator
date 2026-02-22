using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoEventDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("assetId")]
    public string AssetId { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("simulationRunId")]
    public string? SimulationRunId { get; set; }

    [BsonElement("runTick")]
    public int? RunTick { get; set; }

    [BsonElement("relationshipId")]
    public string? RelationshipId { get; set; }

    [BsonElement("occurredAt")]
    public DateTime? OccurredAt { get; set; }

    [BsonElement("payload")]
    public BsonDocument Payload { get; set; } = new BsonDocument();
}
