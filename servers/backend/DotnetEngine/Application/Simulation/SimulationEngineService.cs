using System.Collections.Generic;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using Microsoft.Extensions.Hosting;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// Status=Running인 Run에 대해 주기적으로 due 에셋 수집 후 전파·이벤트를 발생시키는 BackgroundService.
/// </summary>
public sealed class SimulationEngineService : BackgroundService
{
    private const int TickIntervalMs = 1000;
    private const string MetadataTickIntervalMs = "tickIntervalMs";

    private readonly IServiceScopeFactory _scopeFactory;

    public SimulationEngineService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runRepo = scope.ServiceProvider.GetRequiredService<ISimulationRunRepository>();
                var command = scope.ServiceProvider.GetRequiredService<IRunSimulationCommand>();
                var assetRepo = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
                var relRepo = scope.ServiceProvider.GetRequiredService<IRelationshipRepository>();

                var runs = await runRepo.GetRunningAsync(stoppingToken);
                foreach (var run in runs)
                {
                    try
                    {
                        await ProcessRunAsync(run, runRepo, command, assetRepo, relRepo, stoppingToken);
                    }
                    catch
                    {
                        // 한 Run 실패 시 다른 Run 계속 처리
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 루프 단위 예외 시 다음 주기까지 대기
            }

            await Task.Delay(TickIntervalMs, stoppingToken);
        }
    }

    private static async Task ProcessRunAsync(
        SimulationRunDto run,
        ISimulationRunRepository runRepo,
        IRunSimulationCommand command,
        IAssetRepository assetRepo,
        IRelationshipRepository relRepo,
        CancellationToken cancellationToken)
    {
        var participating = await GetParticipatingAssetIdsAsync(run.TriggerAssetId, relRepo, cancellationToken);
        if (participating.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var due = await GetDueAssetIdsAsync(participating, run.StartedAt, assetRepo, cancellationToken);

        var nextTick = run.TickIndex + 1;
        await runRepo.UpdateTickIndexAsync(run.Id, nextTick, cancellationToken);

        if (due.Count == participating.Count)
        {
            // 전역 tick: due = 참여 전체 → RunOnePropagationAsync 1회
            var request = new RunSimulationRequest
            {
                TriggerAssetId = run.TriggerAssetId,
                MaxDepth = run.MaxDepth,
                Patch = null,
                RunTick = nextTick,
            };
            await command.RunOnePropagationAsync(run.Id, request, cancellationToken);
        }
        else
        {
            // due만 처리: due 에셋마다 RunOnePropagationAsync (MaxDepth 0)
            foreach (var assetId in due)
            {
                var request = new RunSimulationRequest
                {
                    TriggerAssetId = assetId,
                    MaxDepth = 0,
                    Patch = null,
                    RunTick = nextTick,
                };
                await command.RunOnePropagationAsync(run.Id, request, cancellationToken);
            }
        }
    }

    private static async Task<HashSet<string>> GetParticipatingAssetIdsAsync(
        string triggerAssetId,
        IRelationshipRepository relRepo,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(triggerAssetId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!visited.Add(id))
                continue;

            var outgoing = await relRepo.GetOutgoingAsync(id, cancellationToken);
            foreach (var rel in outgoing)
            {
                if (!visited.Contains(rel.ToAssetId))
                    queue.Enqueue(rel.ToAssetId);
            }
        }

        return visited;
    }

    private static async Task<List<string>> GetDueAssetIdsAsync(
        HashSet<string> participating,
        DateTimeOffset runStartedAt,
        IAssetRepository assetRepo,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var due = new List<string>();

        foreach (var assetId in participating)
        {
            var asset = await assetRepo.GetByIdAsync(assetId, cancellationToken);
            var state = await assetRepo.GetStateByAssetIdAsync(assetId, cancellationToken);

            var tickIntervalMs = GetTickIntervalMs(asset?.Metadata);
            if (tickIntervalMs <= 0)
            {
                due.Add(assetId);
                continue;
            }

            var lastTick = state?.UpdatedAt ?? runStartedAt;
            var elapsedMs = (now - lastTick).TotalMilliseconds;
            if (elapsedMs >= tickIntervalMs)
                due.Add(assetId);
        }

        return due;
    }

    private static int GetTickIntervalMs(IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue(MetadataTickIntervalMs, out var value))
            return 0;
        if (value is int i)
            return i;
        if (value is long l)
            return (int)l;
        if (value is double d)
            return (int)d;
        return 0;
    }
}
