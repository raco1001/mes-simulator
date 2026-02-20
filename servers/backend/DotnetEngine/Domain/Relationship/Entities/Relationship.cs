namespace DotnetEngine.Domain.Relationship.Entities;

/// <summary>
/// Relationship 엔티티 (에셋 간 관계, MongoDB에서 조회/저장하는 데이터를 표현).
/// </summary>
public sealed class Relationship
{
    public string Id { get; private set; } = string.Empty;
    public string FromAssetId { get; private set; } = string.Empty;
    public string ToAssetId { get; private set; } = string.Empty;
    public string RelationshipType { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, object> Properties { get; private set; } = new Dictionary<string, object>();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Relationship() { }

    /// <summary>
    /// 새 Relationship 엔티티를 생성합니다. (생성 시점에 id, createdAt, updatedAt 설정)
    /// </summary>
    public static Relationship Create(
        string id,
        string fromAssetId,
        string toAssetId,
        string relationshipType,
        IReadOnlyDictionary<string, object>? properties = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Relationship
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)),
            FromAssetId = fromAssetId ?? throw new ArgumentNullException(nameof(fromAssetId)),
            ToAssetId = toAssetId ?? throw new ArgumentNullException(nameof(toAssetId)),
            RelationshipType = relationshipType ?? throw new ArgumentNullException(nameof(relationshipType)),
            Properties = properties ?? new Dictionary<string, object>(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// MongoDB 등에서 복원할 때 사용. (기존 record와 동일한 데이터 주입)
    /// </summary>
    public static Relationship Restore(
        string id,
        string fromAssetId,
        string toAssetId,
        string relationshipType,
        IReadOnlyDictionary<string, object> properties,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new Relationship
        {
            Id = id,
            FromAssetId = fromAssetId,
            ToAssetId = toAssetId,
            RelationshipType = relationshipType,
            Properties = properties ?? new Dictionary<string, object>(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateFromAssetId(string fromAssetId)
    {
        FromAssetId = fromAssetId ?? throw new ArgumentNullException(nameof(fromAssetId));
        TouchUpdatedAt();
    }

    public void UpdateToAssetId(string toAssetId)
    {
        ToAssetId = toAssetId ?? throw new ArgumentNullException(nameof(toAssetId));
        TouchUpdatedAt();
    }

    public void UpdateRelationshipType(string relationshipType)
    {
        RelationshipType = relationshipType ?? throw new ArgumentNullException(nameof(relationshipType));
        TouchUpdatedAt();
    }

    public void UpdateProperties(IReadOnlyDictionary<string, object> properties)
    {
        Properties = properties ?? new Dictionary<string, object>();
        TouchUpdatedAt();
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
