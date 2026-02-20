using System.Text.Json;
using DotnetEngine.Infrastructure.Mongo;
using MongoDB.Bson;
using Xunit;

namespace DotnetEngine.Tests.Infrastructure.Mongo;

public class MetadataBsonConverterTests
{
    [Fact]
    public void ToBsonDocument_NullOrEmpty_ReturnsEmptyBsonDocument()
    {
        var empty = MetadataBsonConverter.ToBsonDocument(null);
        Assert.NotNull(empty);
        Assert.Equal(0, empty.ElementCount);

        var emptyDict = MetadataBsonConverter.ToBsonDocument(new Dictionary<string, object>());
        Assert.NotNull(emptyDict);
        Assert.Equal(0, emptyDict.ElementCount);
    }

    [Fact]
    public void ToBsonDocument_WithJsonElementValues_RoundTripsToDictionary()
    {
        var json = """{"name":"asset1","count":2,"tags":["a","b"],"nested":{"key":"value"}}""";
        using var doc = JsonDocument.Parse(json);
        var dict = new Dictionary<string, object>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            dict[prop.Name] = prop.Value.Clone();

        var bson = MetadataBsonConverter.ToBsonDocument(dict);
        Assert.NotNull(bson);
        Assert.Equal(4, bson.ElementCount);
        Assert.Equal("asset1", bson["name"].AsString);
        Assert.Equal(2, bson["count"].AsInt32);
        Assert.Equal(2, bson["tags"].AsBsonArray.Count);
        Assert.Equal("a", bson["tags"].AsBsonArray[0].AsString);
        Assert.Equal("value", bson["nested"].AsBsonDocument["key"].AsString);

        var back = MetadataBsonConverter.ToDictionary(bson);
        Assert.Equal("asset1", back["name"]);
        Assert.Equal(2, Convert.ToInt32(back["count"]));
        var tags = (List<object>)back["tags"]!;
        Assert.Equal(2, tags.Count);
        Assert.Equal("a", tags[0]);
        Assert.Equal("b", tags[1]);
        var nested = (Dictionary<string, object>)back["nested"]!;
        Assert.Equal("value", nested["key"]);
    }

    [Fact]
    public void ToDictionary_NullOrEmpty_ReturnsEmptyDictionary()
    {
        var empty = MetadataBsonConverter.ToDictionary(null);
        Assert.NotNull(empty);
        Assert.Empty(empty);

        var emptyDoc = MetadataBsonConverter.ToDictionary(new BsonDocument());
        Assert.NotNull(emptyDoc);
        Assert.Empty(emptyDoc);
    }

    [Fact]
    public void ToBsonDocument_WithPlainDotNetTypes_ConvertsCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            ["s"] = "text",
            ["i"] = 42,
            ["b"] = true,
            ["list"] = new List<object> { "x", "y" }
        };

        var bson = MetadataBsonConverter.ToBsonDocument(dict);
        Assert.Equal("text", bson["s"].AsString);
        Assert.Equal(42, bson["i"].AsInt32);
        Assert.True(bson["b"].AsBoolean);
        Assert.Equal(2, bson["list"].AsBsonArray.Count);
    }
}
