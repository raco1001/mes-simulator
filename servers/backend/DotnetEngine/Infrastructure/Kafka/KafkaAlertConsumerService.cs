using System.Text.Json;
using Confluent.Kafka;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetEngine.Infrastructure.Kafka;

/// <summary>
/// Consumes alert events from factory.asset.alert and adds them to IAlertStore.
/// IAlertStore is scoped (MongoAlertStore depends on scoped IMongoDatabase),
/// so we resolve it per-message via IServiceScopeFactory.
/// </summary>
public sealed class KafkaAlertConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAlertNotifier _alertNotifier;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaAlertConsumerService> _logger;

    public KafkaAlertConsumerService(
        IServiceScopeFactory scopeFactory,
        IAlertNotifier alertNotifier,
        IOptions<KafkaOptions> options,
        ILogger<KafkaAlertConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _alertNotifier = alertNotifier;
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
        consumer.Subscribe(_options.TopicAlertEvents);

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
                    await ProcessMessageAsync(json, stoppingToken);
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

    public async Task ProcessMessageAsync(string json, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var alert = MapToAlertDto(root);
        if (alert == null)
            return;

        using var scope = _scopeFactory.CreateScope();
        var alertStore = scope.ServiceProvider.GetRequiredService<IAlertStore>();
        alertStore.Add(alert);
        await _alertNotifier.NotifyAsync(alert, ct);
        _logger.LogDebug("Consumed alert for asset {AssetId}, severity {Severity}", alert.AssetId, alert.Severity);
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

        var metrics = new List<AlertMetricDto>();
        if (payloadProp.TryGetProperty("metrics", out var metricsProp) && metricsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in metricsProp.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                if (!item.TryGetProperty("metric", out var mName) || mName.ValueKind != JsonValueKind.String)
                    continue;
                if (!item.TryGetProperty("current", out var mCurrent) || mCurrent.ValueKind != JsonValueKind.Number)
                    continue;
                if (!item.TryGetProperty("threshold", out var mThreshold) || mThreshold.ValueKind != JsonValueKind.Number)
                    continue;
                if (!item.TryGetProperty("code", out var mCode) || mCode.ValueKind != JsonValueKind.String)
                    continue;
                var mSeverity = item.TryGetProperty("severity", out var mSev) && mSev.ValueKind == JsonValueKind.String
                    ? mSev.GetString() ?? "warning"
                    : "warning";

                metrics.Add(new AlertMetricDto
                {
                    Metric = mName.GetString() ?? "",
                    Current = mCurrent.GetDouble(),
                    Threshold = mThreshold.GetDouble(),
                    Code = mCode.GetString() ?? "",
                    Severity = mSeverity
                });
            }
        }

        if (metrics.Count > 0)
        {
            // Keep legacy fields populated for backward compatibility.
            var first = metrics[0];
            metric = first.Metric;
            current = first.Current;
            threshold = first.Threshold;
            code = first.Code;
        }

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
            Metrics = metrics,
            Metadata = metadata,
        };
    }
}
