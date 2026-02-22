using System.Text.Json;
using Confluent.Kafka;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetEngine.Infrastructure.Kafka;

/// <summary>
/// Consumes alert.generated events from factory.asset.events and adds them to IAlertStore.
/// </summary>
public sealed class KafkaAlertConsumerService : BackgroundService
{
    private const string AlertGeneratedEventType = "alert.generated";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAlertStore _alertStore;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaAlertConsumerService> _logger;

    public KafkaAlertConsumerService(
        IAlertStore alertStore,
        IOptions<KafkaOptions> options,
        ILogger<KafkaAlertConsumerService> logger)
    {
        _alertStore = alertStore;
        _options = options?.Value ?? new KafkaOptions();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_options.TopicAssetEvents);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result?.Message?.Value == null)
                        continue;

                    var json = result.Message.Value;
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("eventType", out var eventTypeProp) ||
                        eventTypeProp.GetString() != AlertGeneratedEventType)
                        continue;

                    var alert = MapToAlertDto(root);
                    if (alert != null)
                    {
                        _alertStore.Add(alert);
                        _logger.LogDebug("Consumed alert for asset {AssetId}, severity {Severity}", alert.AssetId, alert.Severity);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error");
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON in Kafka message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Alert consumer error");
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        await Task.CompletedTask;
    }

    private static AlertDto? MapToAlertDto(JsonElement root)
    {
        if (!root.TryGetProperty("assetId", out var assetIdProp) ||
            !root.TryGetProperty("timestamp", out var timestampProp) ||
            !root.TryGetProperty("payload", out var payloadProp) ||
            !payloadProp.TryGetProperty("severity", out var severityProp) ||
            !payloadProp.TryGetProperty("message", out var messageProp))
            return null;

        var assetId = assetIdProp.GetString() ?? "";
        var timestampStr = timestampProp.GetString();
        if (!DateTimeOffset.TryParse(timestampStr, out var timestamp))
            timestamp = DateTimeOffset.UtcNow;

        var severity = severityProp.GetString() ?? "info";
        var message = messageProp.GetString() ?? "";

        string? runId = null;
        if (payloadProp.TryGetProperty("metadata", out var meta) && meta.TryGetProperty("runId", out var runIdProp))
            runId = runIdProp.GetString();

        string? metric = null;
        if (payloadProp.TryGetProperty("metric", out var metricProp))
            metric = metricProp.GetString();

        double? current = null;
        if (payloadProp.TryGetProperty("current", out var currentProp) && currentProp.ValueKind == JsonValueKind.Number)
            current = currentProp.GetDouble();

        double? threshold = null;
        if (payloadProp.TryGetProperty("threshold", out var thresholdProp) && thresholdProp.ValueKind == JsonValueKind.Number)
            threshold = thresholdProp.GetDouble();

        string? code = null;
        if (payloadProp.TryGetProperty("code", out var codeProp))
            code = codeProp.GetString();

        var metadata = new Dictionary<string, object>();
        if (payloadProp.TryGetProperty("metadata", out var metadataProp) && metadataProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in metadataProp.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.String)
                    metadata[p.Name] = p.Value.GetString()!;
                else if (p.Value.ValueKind == JsonValueKind.Number)
                    metadata[p.Name] = p.Value.GetDouble();
            }
        }

        return new AlertDto
        {
            AssetId = assetId,
            Timestamp = timestamp,
            Severity = severity,
            Message = message,
            RunId = runId,
            Metric = metric,
            Current = current,
            Threshold = threshold,
            Code = code,
            Metadata = metadata,
        };
    }
}
