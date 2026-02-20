
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// MongoDB Asset Repository 구현 (Adapter - Secondary/Driven).
/// </summary>
public sealed class MongoAssetRepository : IAssetRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<MongoAssetDocument> _assetsCollection;
    private readonly IMongoCollection<MongoAssetStateDocument> _statesCollection;

    public MongoAssetRepository(IMongoDatabase database)
    {
        _database = database;
        _assetsCollection = _database.GetCollection<MongoAssetDocument>("assets");
        _statesCollection = _database.GetCollection<MongoAssetStateDocument>("states");
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _assetsCollection.FindAsync(FilterDefinition<MongoAssetDocument>.Empty, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return new List<AssetDto>();
        }
        return documents.Select(ToAssetDto).ToList();
    }

    public async Task<AssetDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoAssetDocument>.Filter.Eq(d => d.Id, id);
        var document = await _assetsCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : ToAssetDto(document);
    }

    public async Task<AssetDto> AddAsync(AssetDto assetDto, CancellationToken cancellationToken = default)
    {
        var document = ToAssetDocument(assetDto);
        await _assetsCollection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToAssetDto(document);
    }

    public async Task<AssetDto?> UpdateAsync(string id, AssetDto assetDto, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoAssetDocument>.Filter.Eq(d => d.Id, id);
        var document = ToAssetDocument(assetDto);
        var result = await _assetsCollection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false }, cancellationToken);
        return result.ModifiedCount > 0 ? ToAssetDto(document) : null;
    }

    public async Task<IReadOnlyList<StateDto>> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var cursor = await _statesCollection.FindAsync(FilterDefinition<MongoAssetStateDocument>.Empty, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return new List<StateDto>();
        }
        return documents.Select(ToStateDto).ToList();
    }

    public async Task<StateDto?> GetStateByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoAssetStateDocument>.Filter.Eq(d => d.AssetId, assetId);
        var document = await _statesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : ToStateDto(document);
    }

    public async Task UpsertStateAsync(StateDto state, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoAssetStateDocument>.Filter.Eq(d => d.AssetId, state.AssetId);
        var existing = await _statesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        var doc = ToStateDocument(state);
        if (existing != null)
        {
            doc.Id = existing.Id;
            await _statesCollection.ReplaceOneAsync(filter, doc, cancellationToken: cancellationToken);
        }
        else
        {
            doc.Id = state.AssetId;
            await _statesCollection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
    }

    private static AssetDto ToAssetDto(MongoAssetDocument doc)
    {
        return new AssetDto
        {
            Id = doc.Id,
            Type = doc.Type,
            Connections = doc.Connections,
            Metadata = MetadataBsonConverter.ToDictionary(doc.Metadata),
            CreatedAt = ToDateTimeOffset(doc.CreatedAt),
            UpdatedAt = ToDateTimeOffset(doc.UpdatedAt)
        };
    }

    private static MongoAssetDocument ToAssetDocument(AssetDto dto)
    {
        return new MongoAssetDocument
        {
            Id = dto.Id,
            Type = dto.Type,
            Connections = dto.Connections,
            Metadata = MetadataBsonConverter.ToBsonDocument(dto.Metadata),
            CreatedAt = dto.CreatedAt.UtcDateTime,
            UpdatedAt = dto.UpdatedAt.UtcDateTime
        };
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static MongoAssetStateDocument ToStateDocument(StateDto dto)
    {
        return new MongoAssetStateDocument
        {
            Id = dto.AssetId,
            AssetId = dto.AssetId,
            CurrentTemp = dto.CurrentTemp,
            CurrentPower = dto.CurrentPower,
            Status = dto.Status,
            LastEventType = dto.LastEventType,
            UpdatedAt = dto.UpdatedAt.UtcDateTime,
            Metadata = MetadataBsonConverter.ToBsonDocument(dto.Metadata)
        };
    }

    private static StateDto ToStateDto(MongoAssetStateDocument doc)
    {
        return new StateDto
        {
            AssetId = doc.AssetId,
            CurrentTemp = doc.CurrentTemp,
            CurrentPower = doc.CurrentPower,
            Status = doc.Status,
            LastEventType = doc.LastEventType,
            UpdatedAt = ToDateTimeOffset(doc.UpdatedAt),
            Metadata = MetadataBsonConverter.ToDictionary(doc.Metadata)
        };
    }
}
