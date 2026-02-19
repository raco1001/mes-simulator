using MongoDB.Bson.Serialization.Attributes;

namespace DotnetEngine.Infrastructure.Mongo;

public sealed class MongoAssetDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public IReadOnlyList<string> Connections { get; set; } = [];
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}