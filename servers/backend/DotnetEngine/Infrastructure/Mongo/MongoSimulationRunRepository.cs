using DotnetEngine.Application.Simulation;
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
            TickIndex = doc.TickIndex,
        };
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
            TickIndex = dto.TickIndex,
        };
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime dt)
    {
        var utc = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
