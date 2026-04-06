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
    /// <summary>파이프라인(Phase 21)이 Mongo에 쓰는 운영 상태. 없으면 null.</summary>
    [BsonElement("operationalStatus")]
    public string? OperationalStatus { get; set; }
    /// <summary>시뮬 전용 상태(파이프라인). 없으면 null.</summary>
    [BsonElement("simulationStatus")]
    public string? SimulationStatus { get; set; }
    [BsonElement("lastEventType")]
    public string? LastEventType { get; set; }
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = new BsonDocument();
}
