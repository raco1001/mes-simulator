using System.Text.Json;
using Confluent.Kafka;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetEngine.Infrastructure.Kafka;

/// <summary>
/// EventDto를 Kafka factory.asset.events 토픽으로 발행. Pipeline 기대 형식: eventType, assetId, timestamp, payload.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger)
    {
        var opts = options?.Value ?? new KafkaOptions();
        _topic = opts.TopicAssetEvents;
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = opts.BootstrapServers,
        };
        _producer = new ProducerBuilder<Null, string>(config)
            .SetKeySerializer(Serializers.Null)
            .Build();
    }

    public async Task PublishAsync(EventDto dto, CancellationToken cancellationToken = default)
    {
        var message = new
        {
            eventType = dto.EventType,
            assetId = dto.AssetId,
            timestamp = dto.OccurredAt.UtcDateTime,
            runId = dto.SimulationRunId,
            payload = dto.Payload,
        };
        var json = JsonSerializer.Serialize(message, JsonOptions);
        try
        {
            await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka publish failed for asset {AssetId}, eventType {EventType}. Event is still persisted.", dto.AssetId, dto.EventType);
        }
    }

    public void Dispose() => _producer?.Dispose();
}
