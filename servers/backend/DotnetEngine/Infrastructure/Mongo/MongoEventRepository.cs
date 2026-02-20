using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// MongoDB Event Repository 구현 (Adapter - Secondary/Driven). Append-only.
/// </summary>
public sealed class MongoEventRepository : IEventRepository
{
    private readonly IMongoCollection<MongoEventDocument> _collection;

    public MongoEventRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoEventDocument>("events");
    }

    public async Task AppendAsync(EventDto dto, CancellationToken cancellationToken = default)
    {
        var document = new MongoEventDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AssetId = dto.AssetId,
            EventType = dto.EventType,
            Timestamp = dto.OccurredAt.UtcDateTime,
            SimulationRunId = dto.SimulationRunId,
            RelationshipId = dto.RelationshipId,
            OccurredAt = dto.OccurredAt.UtcDateTime,
            Payload = MetadataBsonConverter.ToBsonDocument(dto.Payload),
        };
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<EventDto>> GetBySimulationRunIdAsync(string simulationRunId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MongoEventDocument>.Filter.Eq(d => d.SimulationRunId, simulationRunId);
        var sort = Builders<MongoEventDocument>.Sort.Ascending(d => d.OccurredAt).Ascending(d => d.Timestamp);
        var cursor = await _collection.FindAsync(filter, new FindOptions<MongoEventDocument, MongoEventDocument> { Sort = sort }, cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    private static EventDto ToDto(MongoEventDocument doc)
    {
        var occurredAt = doc.OccurredAt ?? doc.Timestamp;
        return new EventDto
        {
            AssetId = doc.AssetId,
            EventType = doc.EventType,
            OccurredAt = new DateTimeOffset(occurredAt, TimeSpan.Zero),
            SimulationRunId = doc.SimulationRunId,
            RelationshipId = doc.RelationshipId,
            Payload = MetadataBsonConverter.ToDictionary(doc.Payload),
        };
    }
}
