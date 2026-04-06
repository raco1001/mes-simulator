using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation;
using DotnetEngine.Domain.Simulation.ValueObjects;
using DotnetEngine.Application.Simulation.Ports.Driven;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// MongoDB SimulationRun Repository 구현 (Adapter - Secondary/Driven).
/// </summary>
public sealed class MongoSimulationRunRepository : ISimulationRunRepository
{
    private readonly IMongoCollection<MongoSimulationRunDocument> _collection;

    public MongoSimulationRunRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoSimulationRunDocument>("simulation_runs");
    }

    public async Task<SimulationRunDto> CreateAsync(SimulationRunDto dto, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(dto);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToDto(document);
    }

    public async Task<SimulationRunDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDto(document);
    }

    public async Task<IReadOnlyList<SimulationRunDto>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Status, "Running");
        var documents = await _collection.Find(filter).ToListAsync(cancellationToken);
        return documents.ConvertAll(ToDto);
    }

    public async Task UpdateStatusAsync(string id, SimulationRunStatus status, DateTimeOffset? endedAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<MongoSimulationRunDocument>.Update.Set(d => d.Status, status.ToString());
        if (endedAt.HasValue)
            update = update.Set(d => d.EndedAt, endedAt.Value.UtcDateTime);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task UpdateTickIndexAsync(string id, int tickIndex, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<MongoSimulationRunDocument>.Update.Set(d => d.TickIndex, tickIndex);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task EndAsync(string id, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<MongoSimulationRunDocument>.Update
            .Set(d => d.Status, SimulationRunStatus.Completed.ToString())
            .Set(d => d.EndedAt, endedAt.UtcDateTime);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task ReplaceInitialSnapshotAsync(string id, IReadOnlyDictionary<string, object> snapshot, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var bson = SnapshotToBson(snapshot);
        var update = Builders<MongoSimulationRunDocument>.Update.Set(d => d.InitialSnapshot, bson);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task AppendOverrideAsync(string id, SimulationOverrideEntryDto entry, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var doc = new BsonDocument
        {
            ["assetId"] = entry.AssetId,
            ["propertyKey"] = entry.PropertyKey,
            ["value"] = MetadataBsonConverter.ToBsonValue(entry.Value) ?? BsonNull.Value,
            ["fromTick"] = entry.FromTick,
        };
        if (entry.ToTick.HasValue)
            doc["toTick"] = entry.ToTick.Value;

        var update = Builders<MongoSimulationRunDocument>.Update.Push(d => d.Overrides, doc);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static BsonDocument SnapshotToBson(IReadOnlyDictionary<string, object> snapshot)
    {
        var bson = new BsonDocument();
        foreach (var kv in snapshot)
        {
            var v = MetadataBsonConverter.ToBsonValue(kv.Value);
            if (v != null)
                bson[kv.Key] = v;
        }
        return bson;
    }

    private static SimulationRunDto ToDto(MongoSimulationRunDocument doc)
    {
        var status = string.IsNullOrEmpty(doc.Status) || !Enum.TryParse<SimulationRunStatus>(doc.Status, ignoreCase: true, out var s)
            ? SimulationRunStatus.Pending
            : s;
        return new SimulationRunDto
        {
            Id = doc.Id,
            Status = status,
            StartedAt = ToDateTimeOffset(doc.StartedAt),
            EndedAt = doc.EndedAt.HasValue ? ToDateTimeOffset(doc.EndedAt.Value) : null,
            TriggerAssetId = doc.TriggerAssetId,
            Trigger = MetadataBsonConverter.ToDictionary(doc.Trigger),
            MaxDepth = doc.MaxDepth,
            EngineTickIntervalMs = doc.EngineTickIntervalMs <= 0
                ? SimulationEngineConstants.DefaultEngineTickIntervalMs
                : SimulationEngineConstants.ClampEngineTickIntervalMs(doc.EngineTickIntervalMs),
            TickIndex = doc.TickIndex,
            InitialSnapshot = MetadataBsonConverter.ToDictionary(doc.InitialSnapshot),
            Overrides = ToOverrideDtos(doc.Overrides),
        };
    }

    private static IReadOnlyList<SimulationOverrideEntryDto> ToOverrideDtos(List<BsonDocument> list)
    {
        if (list.Count == 0)
            return Array.Empty<SimulationOverrideEntryDto>();

        var result = new List<SimulationOverrideEntryDto>(list.Count);
        foreach (var b in list)
        {
            if (!b.TryGetValue("assetId", out var av) || av.IsBsonNull)
                continue;
            if (!b.TryGetValue("propertyKey", out var pk) || pk.IsBsonNull)
                continue;
            if (!b.TryGetValue("fromTick", out var ft) || !(ft.IsInt32 || ft.IsInt64))
                continue;
            var val = b.GetValue("value", BsonNull.Value);
            var obj = MetadataBsonConverter.ToObject(val);
            var fromTick = ft.IsInt32 ? ft.AsInt32 : (int)ft.AsInt64;
            int? toTick = null;
            if (b.TryGetValue("toTick", out var tt) && tt is not null && !tt.IsBsonNull && (tt.IsInt32 || tt.IsInt64))
                toTick = tt.IsInt32 ? tt.AsInt32 : (int)tt.AsInt64;

            result.Add(new SimulationOverrideEntryDto
            {
                AssetId = av.AsString,
                PropertyKey = pk.AsString,
                Value = obj ?? "",
                FromTick = fromTick,
                ToTick = toTick,
            });
        }

        return result;
    }

    private static MongoSimulationRunDocument ToDocument(SimulationRunDto dto)
    {
        return new MongoSimulationRunDocument
        {
            Id = dto.Id,
            Status = dto.Status.ToString(),
            StartedAt = dto.StartedAt.UtcDateTime,
            EndedAt = dto.EndedAt?.UtcDateTime,
            TriggerAssetId = dto.TriggerAssetId,
            Trigger = MetadataBsonConverter.ToBsonDocument(dto.Trigger),
            MaxDepth = dto.MaxDepth,
            EngineTickIntervalMs = SimulationEngineConstants.ClampEngineTickIntervalMs(dto.EngineTickIntervalMs),
            TickIndex = dto.TickIndex,
            InitialSnapshot = SnapshotToBson(dto.InitialSnapshot),
            Overrides = dto.Overrides.Select(o => new BsonDocument
            {
                ["assetId"] = o.AssetId,
                ["propertyKey"] = o.PropertyKey,
                ["value"] = MetadataBsonConverter.ToBsonValue(o.Value) ?? BsonNull.Value,
                ["fromTick"] = o.FromTick,
                ["toTick"] = o.ToTick.HasValue ? o.ToTick.Value : BsonNull.Value,
            }).ToList(),
        };
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
