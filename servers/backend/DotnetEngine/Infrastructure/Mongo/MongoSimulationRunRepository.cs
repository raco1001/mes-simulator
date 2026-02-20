using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
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

    public async Task EndAsync(string id, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoSimulationRunDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<MongoSimulationRunDocument>.Update.Set(d => d.EndedAt, endedAt.UtcDateTime);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static SimulationRunDto ToDto(MongoSimulationRunDocument doc)
    {
        return new SimulationRunDto
        {
            Id = doc.Id,
            StartedAt = ToDateTimeOffset(doc.StartedAt),
            EndedAt = doc.EndedAt.HasValue ? ToDateTimeOffset(doc.EndedAt.Value) : null,
            TriggerAssetId = doc.TriggerAssetId,
            Trigger = MetadataBsonConverter.ToDictionary(doc.Trigger),
            MaxDepth = doc.MaxDepth,
        };
    }

    private static MongoSimulationRunDocument ToDocument(SimulationRunDto dto)
    {
        return new MongoSimulationRunDocument
        {
            Id = dto.Id,
            StartedAt = dto.StartedAt.UtcDateTime,
            EndedAt = dto.EndedAt?.UtcDateTime,
            TriggerAssetId = dto.TriggerAssetId,
            Trigger = MetadataBsonConverter.ToBsonDocument(dto.Trigger),
            MaxDepth = dto.MaxDepth,
        };
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
