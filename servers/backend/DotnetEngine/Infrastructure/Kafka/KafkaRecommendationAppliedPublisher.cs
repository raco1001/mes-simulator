using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using DotnetEngine.Application.Recommendation.Ports.Driven;
using Microsoft.Extensions.Options;

namespace DotnetEngine.Infrastructure.Kafka;

public sealed class KafkaRecommendationAppliedPublisher : IRecommendationAppliedPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;

    public KafkaRecommendationAppliedPublisher(IOptions<KafkaOptions> options)
    {
        var opts = options?.Value ?? new KafkaOptions();
        _topic = opts.TopicRecommendationAppliedEvents;
        var config = new ProducerConfig
        {
            BootstrapServers = opts.BootstrapServers,
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync(
        string recommendationId,
        string triggerAssetId,
        IReadOnlyDictionary<string, object?> patch,
        string runId,
        DateTimeOffset appliedAt,
        CancellationToken cancellationToken = default)
    {
        var message = new
        {
            eventType = "recommendation.applied",
            assetId = triggerAssetId,
            timestamp = appliedAt.UtcDateTime,
            schemaVersion = "v1",
            runId,
            payload = new
            {
                recommendationId,
                status = "applied",
                triggerAssetId,
                patch,
                runId,
                appliedAt = appliedAt.UtcDateTime
            }
        };
        var json = JsonSerializer.Serialize(message, JsonOptions);
        await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json }, cancellationToken);
    }

    public void Dispose() => _producer?.Dispose();
}
