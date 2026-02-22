using System.Collections.Concurrent;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using Microsoft.Extensions.Options;

namespace DotnetEngine.Infrastructure.Alert;

/// <summary>
/// Thread-safe in-memory alert store. Keeps the latest N alerts (newest first).
/// </summary>
public sealed class InMemoryAlertStore : IAlertStore
{
    private const int DefaultMaxAlerts = 100;

    private readonly int _maxAlerts;
    private readonly ConcurrentQueue<AlertDto> _queue = new();
    private readonly object _lock = new();

    public InMemoryAlertStore(IOptions<InMemoryAlertStoreOptions>? options = null)
    {
        _maxAlerts = options?.Value?.MaxAlerts ?? DefaultMaxAlerts;
    }

    public void Add(AlertDto alert)
    {
        lock (_lock)
        {
            _queue.Enqueue(alert);
            while (_queue.Count > _maxAlerts && _queue.TryDequeue(out _)) { }
        }
    }

    public IReadOnlyList<AlertDto> GetLatest(int maxCount)
    {
        lock (_lock)
        {
            var list = _queue.ToArray();
            var take = Math.Min(maxCount, list.Length);
            return list
                .OrderByDescending(a => a.Timestamp)
                .Take(take)
                .ToList();
        }
    }
}

/// <summary>
/// Options for InMemoryAlertStore.
/// </summary>
public sealed class InMemoryAlertStoreOptions
{
    public int MaxAlerts { get; set; } = 100;
}
