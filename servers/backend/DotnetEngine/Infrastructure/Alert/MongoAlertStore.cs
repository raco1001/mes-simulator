using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using DotnetEngine.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DotnetEngine.Infrastructure.Alert;

/// <summary>
/// MongoDB-backed alert store implementation.
/// </summary>
public sealed class MongoAlertStore : IAlertStore
{
    private readonly IMongoCollection<MongoAlertDocument> _collection;

    public MongoAlertStore(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoAlertDocument>("alerts");
    }

    public void Add(AlertDto alert)
    {
        var document = ToDocument(alert);
        _collection.InsertOne(document);
    }

    public IReadOnlyList<AlertDto> GetLatest(int maxCount)
    {
        if (maxCount <= 0)
            return new List<AlertDto>();

        var options = new FindOptions<MongoAlertDocument>
        {
            Sort = Builders<MongoAlertDocument>.Sort.Descending(d => d.Timestamp),
            Limit = maxCount,
        };

        using var cursor = _collection.FindSync(FilterDefinition<MongoAlertDocument>.Empty, options);
        var documents = cursor.ToList();

        return documents.Select(ToDto).ToList();
    }

    private static MongoAlertDocument ToDocument(AlertDto dto)
    {
        return new MongoAlertDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AssetId = dto.AssetId,
            Timestamp = dto.Timestamp.UtcDateTime,
            Severity = dto.Severity,
            Message = dto.Message,
            RunId = dto.RunId,
            Metric = dto.Metric,
            Current = dto.Current,
            Threshold = dto.Threshold,
            Code = dto.Code,
            Metadata = MetadataBsonConverter.ToBsonDocument(dto.Metadata),
        };
    }

    private static AlertDto ToDto(MongoAlertDocument document)
    {
        return new AlertDto
        {
            AssetId = document.AssetId,
            Timestamp = new DateTimeOffset(document.Timestamp, TimeSpan.Zero),
            Severity = document.Severity,
            Message = document.Message,
            RunId = document.RunId,
            Metric = document.Metric,
            Current = document.Current,
            Threshold = document.Threshold,
            Code = document.Code,
            Metadata = MetadataBsonConverter.ToDictionary(document.Metadata),
        };
    }
}
