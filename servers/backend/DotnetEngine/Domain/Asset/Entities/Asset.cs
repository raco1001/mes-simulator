namespace DotnetEngine.Domain.Asset.Entities;

/// <summary>
/// Asset 엔티티 (MongoDB에서 조회/저장하는 데이터를 표현).
/// </summary>
public sealed class Asset
{
    public string Id { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public IReadOnlyList<string> Connections { get; private set; } = [];
    public IReadOnlyDictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Asset() { }

    /// <summary>
    /// 새 Asset 엔티티를 생성합니다. (생성 시점에 id, createdAt, updatedAt 설정)
    /// </summary>
    public static Asset Create(
        string id,
        string type,
        IReadOnlyList<string>? connections = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Asset
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)),
            Type = type ?? throw new ArgumentNullException(nameof(type)),
            Connections = connections ?? [],
            Metadata = metadata ?? new Dictionary<string, object>(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// MongoDB 등에서 복원할 때 사용. (기존 record와 동일한 데이터 주입)
    /// </summary>
    public static Asset Restore(
        string id,
        string type,
        IReadOnlyList<string> connections,
        IReadOnlyDictionary<string, object> metadata,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new Asset
        {
            Id = id,
            Type = type,
            Connections = connections ?? [],
            Metadata = metadata ?? new Dictionary<string, object>(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateType(string type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        TouchUpdatedAt();
    }

    public void UpdateConnections(IReadOnlyList<string> connections)
    {
        Connections = connections ?? [];
        TouchUpdatedAt();
    }

    public void UpdateMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        Metadata = metadata ?? new Dictionary<string, object>();
        TouchUpdatedAt();
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
