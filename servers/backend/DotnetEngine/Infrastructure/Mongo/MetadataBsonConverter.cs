using System.Text.Json;
using MongoDB.Bson;

namespace DotnetEngine.Infrastructure.Mongo;

/// <summary>
/// Converts API DTO Metadata (Dictionary with possible JsonElement values) to/from BsonDocument
/// so that MongoDB can serialize it without JsonElement.
/// </summary>
public static class MetadataBsonConverter
{
    /// <summary>
    /// Converts metadata dictionary to BsonDocument. Handles JsonElement and other .NET types.
    /// </summary>
    public static BsonDocument ToBsonDocument(IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata == null || metadata.Count == 0)
            return new BsonDocument();

        var doc = new BsonDocument();
        foreach (var kv in metadata)
        {
            var value = ToBsonValue(kv.Value);
            if (value != null)
                doc[kv.Key] = value;
        }
        return doc;
    }

    /// <summary>
    /// Converts BsonDocument to dictionary for API response (JSON-serializable).
    /// </summary>
    public static IReadOnlyDictionary<string, object> ToDictionary(BsonDocument? doc)
    {
        if (doc == null || doc.ElementCount == 0)
            return new Dictionary<string, object>();

        var dict = new Dictionary<string, object>();
        foreach (var element in doc.Elements)
        {
            var value = ToObject(element.Value);
            if (value != null)
                dict[element.Name] = value;
        }
        return dict;
    }

    private static BsonValue? ToBsonValue(object? value)
    {
        if (value == null)
            return BsonNull.Value;

        if (value is JsonElement je)
            return JsonElementToBsonValue(je);

        if (value is string s)
            return new BsonString(s);
        if (value is int i)
            return new BsonInt32(i);
        if (value is long l)
            return new BsonInt64(l);
        if (value is double d)
            return new BsonDouble(d);
        if (value is bool b)
            return new BsonBoolean(b);

        if (value is IReadOnlyDictionary<string, object> dict)
        {
            var sub = new BsonDocument();
            foreach (var kv in dict)
            {
                var v = ToBsonValue(kv.Value);
                if (v != null)
                    sub[kv.Key] = v;
            }
            return sub;
        }

        if (value is IEnumerable<object> list)
        {
            var arr = new BsonArray();
            foreach (var item in list)
            {
                var v = ToBsonValue(item);
                if (v != null)
                    arr.Add(v);
            }
            return arr;
        }

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var arr = new BsonArray();
            foreach (var item in enumerable)
            {
                var v = ToBsonValue(item);
                if (v != null)
                    arr.Add(v);
            }
            return arr;
        }

        return null;
    }

    private static BsonValue JsonElementToBsonValue(JsonElement je)
    {
        return je.ValueKind switch
        {
            JsonValueKind.String => new BsonString(je.GetString() ?? string.Empty),
            JsonValueKind.Number => je.TryGetInt32(out var i) ? new BsonInt32(i) : new BsonDouble(je.GetDouble()),
            JsonValueKind.True => new BsonBoolean(true),
            JsonValueKind.False => new BsonBoolean(false),
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.Object => JsonObjectToBsonDocument(je),
            JsonValueKind.Array => JsonArrayToBsonArray(je),
            _ => BsonNull.Value
        };
    }

    private static BsonDocument JsonObjectToBsonDocument(JsonElement je)
    {
        var doc = new BsonDocument();
        foreach (var prop in je.EnumerateObject())
        {
            doc[prop.Name] = JsonElementToBsonValue(prop.Value);
        }
        return doc;
    }

    private static BsonArray JsonArrayToBsonArray(JsonElement je)
    {
        var arr = new BsonArray();
        foreach (var item in je.EnumerateArray())
        {
            arr.Add(JsonElementToBsonValue(item));
        }
        return arr;
    }

    private static object? ToObject(BsonValue bv)
    {
        if (bv == null || bv.IsBsonNull)
            return null;

        if (bv.IsString)
            return bv.AsString;
        if (bv.IsInt32)
            return bv.AsInt32;
        if (bv.IsInt64)
            return bv.AsInt64;
        if (bv.IsDouble)
            return bv.AsDouble;
        if (bv.IsBoolean)
            return bv.AsBoolean;

        if (bv.IsBsonDocument)
        {
            var sub = new Dictionary<string, object>();
            foreach (var element in bv.AsBsonDocument.Elements)
            {
                var v = ToObject(element.Value);
                if (v != null)
                    sub[element.Name] = v;
            }
            return sub;
        }

        if (bv.IsBsonArray)
        {
            var list = new List<object>();
            foreach (var item in bv.AsBsonArray)
            {
                var o = ToObject(item);
                if (o != null)
                    list.Add(o);
            }
            return list;
        }

        return null;
    }
}
