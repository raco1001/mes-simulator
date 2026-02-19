namespace DotnetEngine.Domain.Asset.Entities;

/// <summary>
/// Asset 상태 엔티티 (MongoDB에서 조회한 데이터를 표현).
/// </summary>
public sealed class AssetState
{
    public string AssetId { get; private set; } = string.Empty;
    public double? CurrentTemp { get; private set; }
    public double? CurrentPower { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? LastEventType { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyDictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>();

    private AssetState() { }

    /// <summary>
    /// MongoDB 등에서 복원할 때 사용.
    /// </summary>
    public static AssetState Restore(
        string assetId,
        double? currentTemp,
        double? currentPower,
        string status,
        string? lastEventType,
        DateTimeOffset updatedAt,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new AssetState
        {
            AssetId = assetId,
            CurrentTemp = currentTemp,
            CurrentPower = currentPower,
            Status = status,
            LastEventType = lastEventType,
            UpdatedAt = updatedAt,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    public void UpdateStatus(string status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        TouchUpdatedAt();
    }

    public void UpdateTemp(double? currentTemp)
    {
        CurrentTemp = currentTemp;
        TouchUpdatedAt();
    }

    public void UpdatePower(double? currentPower)
    {
        CurrentPower = currentPower;
        TouchUpdatedAt();
    }

    private void TouchUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
