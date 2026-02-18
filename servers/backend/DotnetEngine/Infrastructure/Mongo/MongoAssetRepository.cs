using DotnetEngine.Domain.Asset.ValueObjects;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// MongoDB Asset Repository 구현 (Adapter - Secondary/Driven).
/// </summary>
public sealed class MongoAssetRepository : IAssetRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<BsonDocument> _assetsCollection;
    private readonly IMongoCollection<BsonDocument> _statesCollection;

    public MongoAssetRepository(IMongoDatabase database)
    {
        _database = database;
        _assetsCollection = _database.GetCollection<BsonDocument>("assets");
        _statesCollection = _database.GetCollection<BsonDocument>("states");
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _assetsCollection.FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);

        return documents.Select(ToAsset).ToList();
    }

    public async Task<Asset?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var document = await _assetsCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document == null ? null : ToAsset(document);
    }

    public async Task<IReadOnlyList<AssetState>> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _statesCollection.FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);

        return documents.Select(ToAssetState).ToList();
    }

    public async Task<AssetState?> GetStateByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("assetId", assetId);
        var document = await _statesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document == null ? null : ToAssetState(document);
    }

    private static Asset ToAsset(BsonDocument doc)
    {
        return new Asset
        {
            Id = doc["_id"].AsString,
            Type = doc["type"].AsString,
            Connections = doc.Contains("connections")
                ? doc["connections"].AsBsonArray.Select(x => x.AsString).ToList()
                : new List<string>(),
            Metadata = doc.Contains("metadata")
                ? doc["metadata"].AsBsonDocument.ToDictionary(
                    x => x.Name,
                    x => (object)x.Value.AsBsonValue)
                : new Dictionary<string, object>(),
            CreatedAt = doc["createdAt"].ToUniversalTime(),
            UpdatedAt = doc["updatedAt"].ToUniversalTime()
        };
    }

    private static AssetState ToAssetState(BsonDocument doc)
    {
        return new AssetState
        {
            AssetId = doc["assetId"].AsString,
            CurrentTemp = doc.Contains("currentTemp") && !doc["currentTemp"].IsBsonNull
                ? doc["currentTemp"].AsDouble
                : null,
            CurrentPower = doc.Contains("currentPower") && !doc["currentPower"].IsBsonNull
                ? doc["currentPower"].AsDouble
                : null,
            Status = doc["status"].AsString,
            LastEventType = doc.Contains("lastEventType") && !doc["lastEventType"].IsBsonNull
                ? doc["lastEventType"].AsString
                : null,
            UpdatedAt = doc["updatedAt"].ToUniversalTime(),
            Metadata = doc.Contains("metadata")
                ? doc["metadata"].AsBsonDocument.ToDictionary(
                    x => x.Name,
                    x => (object)x.Value.AsBsonValue)
                : new Dictionary<string, object>()
        };
    }
}
