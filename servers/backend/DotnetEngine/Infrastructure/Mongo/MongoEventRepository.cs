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
}
