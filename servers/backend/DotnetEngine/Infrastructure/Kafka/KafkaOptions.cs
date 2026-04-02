namespace DotnetEngine.Infrastructure.Kafka;

/// <summary>
/// Kafka producer 설정 (BootstrapServers, Topic).
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicAssetEvents { get; set; } = "factory.asset.events";
    public string TopicAlertEvents { get; set; } = "factory.asset.alert";
    public string TopicRecommendationAppliedEvents { get; set; } = "factory.recommendation.applied";
    /// <summary>Consumer group for alert consumer (subscribes to TopicAlertEvents).</summary>
    public string ConsumerGroupId { get; set; } = "backend-alert-consumer";
}
