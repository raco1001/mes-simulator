using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Infrastructure.Alert;
using DotnetEngine.Infrastructure.Mongo;
using Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace DotnetEngine.Tests.Infrastructure.Mongo;

public class MongoAlertStoreTests
{
    [Fact]
    public void Add_InsertsMappedAlertDocument()
    {
        var collection = new Mock<IMongoCollection<MongoAlertDocument>>();
        var database = new Mock<IMongoDatabase>();
        database
            .Setup(d => d.GetCollection<MongoAlertDocument>("alerts", null))
            .Returns(collection.Object);
        MongoAlertDocument? inserted = null;
        collection
            .Setup(c => c.InsertOne(It.IsAny<MongoAlertDocument>(), null, default))
            .Callback<MongoAlertDocument, InsertOneOptions?, CancellationToken>((doc, _, _) => inserted = doc);

        var sut = new MongoAlertStore(database.Object);
        var alert = new AlertDto
        {
            AssetId = "freezer-1",
            Timestamp = new DateTimeOffset(2026, 3, 25, 10, 20, 30, TimeSpan.Zero),
            Severity = "warning",
            Message = "temperature warning",
            RunId = "run-1",
            Metric = "temperature",
            Current = -5.0,
            Threshold = -10.0,
            Code = "TEMP_HIGH",
            Metadata = new Dictionary<string, object> { ["source"] = "pipeline" },
        };

        sut.Add(alert);

        collection.Verify(c => c.InsertOne(It.IsAny<MongoAlertDocument>(), null, default), Times.Once);
        Assert.NotNull(inserted);
        Assert.Equal("freezer-1", inserted!.AssetId);
        Assert.Equal("warning", inserted.Severity);
        Assert.Equal("temperature warning", inserted.Message);
        Assert.Equal("run-1", inserted.RunId);
        Assert.Equal("pipeline", inserted.Metadata["source"].AsString);
    }

    [Fact]
    public void GetLatest_ReturnsMappedDtos_AndRespectsLimit()
    {
        var collection = new Mock<IMongoCollection<MongoAlertDocument>>();
        var database = new Mock<IMongoDatabase>();
        database
            .Setup(d => d.GetCollection<MongoAlertDocument>("alerts", null))
            .Returns(collection.Object);
        var docs = new List<MongoAlertDocument>
        {
            new()
            {
                AssetId = "freezer-2",
                Timestamp = new DateTime(2026, 3, 25, 10, 25, 00, DateTimeKind.Utc),
                Severity = "error",
                Message = "engine failure",
                Metadata = new BsonDocument("source", "pipeline"),
            },
            new()
            {
                AssetId = "freezer-1",
                Timestamp = new DateTime(2026, 3, 25, 10, 20, 00, DateTimeKind.Utc),
                Severity = "warning",
                Message = "temperature warning",
                Metadata = new BsonDocument("source", "pipeline"),
            },
        };
        var cursor = new Mock<IAsyncCursor<MongoAlertDocument>>();
        cursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        cursor.SetupGet(c => c.Current).Returns(docs);

        collection
            .Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<MongoAlertDocument>>(),
                It.IsAny<FindOptions<MongoAlertDocument, MongoAlertDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns(cursor.Object);

        var sut = new MongoAlertStore(database.Object);

        var result = sut.GetLatest(2);

        collection.Verify(
            c => c.FindSync(
                It.IsAny<FilterDefinition<MongoAlertDocument>>(),
                It.Is<FindOptions<MongoAlertDocument, MongoAlertDocument>>(o => o.Limit == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(2, result.Count);
        Assert.Equal("freezer-2", result[0].AssetId);
        Assert.Equal("error", result[0].Severity);
        Assert.Equal("freezer-1", result[1].AssetId);
        Assert.Equal("warning", result[1].Severity);
        Assert.Equal("pipeline", result[0].Metadata["source"]);
    }
}
