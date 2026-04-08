using System.Collections.Generic;
using System.Linq;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Domain.Simulation;
using DotnetEngine.Domain.Simulation.Constants;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetEngine.Application.Simulation.Workers;

/// <summary>
/// Status=Running인 Run에 대해 주기적으로 due 에셋 수집 후 전파·이벤트를 발생시키는 BackgroundService.
/// </summary>
public sealed class SimulationEngineService : BackgroundService
{
    private const string MetadataTickIntervalMs = "tickIntervalMs";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimulationNotifier _simulationNotifier;
    private readonly ILogger<SimulationEngineService> _logger;

    public SimulationEngineService(
        IServiceScopeFactory scopeFactory,
        ISimulationNotifier simulationNotifier,
        ILogger<SimulationEngineService> logger)
    {
        _scopeFactory = scopeFactory;
        _simulationNotifier = simulationNotifier;
        _logger = logger;
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
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                var runs = await runRepo.GetRunningAsync(stoppingToken);
                foreach (var run in runs)
                {
                    try
                    {
                        await ProcessRunAsync(
                            run,
                            runRepo,
                            command,
                            assetRepo,
                            relRepo,
                            eventPublisher,
                            _simulationNotifier,
                            _logger,
                            stoppingToken);
                    }
                    catch
                    {
                        // 한 Run 실패 시 다른 Run 계속 처리
                    }
                }

                var delayMs = runs.Count == 0
                    ? SimulationEngineConstants.DefaultEngineTickIntervalMs
                    : runs.Min(r => SimulationEngineConstants.ClampEngineTickIntervalMs(r.EngineTickIntervalMs));
                await Task.Delay(delayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(SimulationEngineConstants.DefaultEngineTickIntervalMs, stoppingToken);
            }
        }
    }

    private static async Task ProcessRunAsync(
        SimulationRunDto run,
        ISimulationRunRepository runRepo,
        IRunSimulationCommand command,
        IAssetRepository assetRepo,
        IRelationshipRepository relRepo,
        IEventPublisher eventPublisher,
        ISimulationNotifier notifier,
        ILogger<SimulationEngineService> logger,
        CancellationToken cancellationToken)
    {
        var participating = await SimulationParticipation.GetParticipatingAssetIdsAsync(run.TriggerAssetIds, relRepo, cancellationToken);
        if (participating.Count == 0)
            return;

        var due = await GetDueAssetIdsAsync(participating, run.StartedAt, assetRepo, cancellationToken);
        var nextTick = run.TickIndex + 1;
        await runRepo.UpdateTickIndexAsync(run.Id, nextTick, cancellationToken);

        var dueList = due.ToList();
        var sequentialSingleSeed = dueList.Count != participating.Count;
        logger.LogDebug(
            "SimulationEngine tick {Tick} run {RunId}: participating={ParticipatingCount} due={DueCount} sequentialSingleSeed={SequentialSingleSeed} dueIds=[{DueIds}]",
            nextTick,
            run.Id,
            participating.Count,
            dueList.Count,
            sequentialSingleSeed,
            string.Join(',', dueList));

        await PublishTickEnvelopeAsync(
            eventPublisher,
            EventTypes.SimulationTickStarted,
            run.Id,
            nextTick,
            dueList,
            changedAssetIds: null,
            cancellationToken);

        var changed = new HashSet<string>(StringComparer.Ordinal);
        if (dueList.Count == participating.Count)
        {
            var request = new RunSimulationRequest
            {
                TriggerAssetIds = run.TriggerAssetIds.ToList(),
                MaxDepth = run.MaxDepth,
                Patch = null,
                RunTick = nextTick,
                EngineTickIntervalMs = SimulationEngineConstants.ClampEngineTickIntervalMs(run.EngineTickIntervalMs),
            };
            var outcome = await command.RunOnePropagationAsync(run.Id, request, cancellationToken: cancellationToken);
            foreach (var id in outcome.ChangedAssetIds)
                changed.Add(id);
        }
        else
        {
            foreach (var assetId in dueList)
            {
                var request = new RunSimulationRequest
                {
                    TriggerAssetId = assetId,
                    MaxDepth = run.MaxDepth,
                    Patch = null,
                    RunTick = nextTick,
                    EngineTickIntervalMs = SimulationEngineConstants.ClampEngineTickIntervalMs(run.EngineTickIntervalMs),
                };
                var outcome = await command.RunOnePropagationAsync(run.Id, request, cancellationToken: cancellationToken);
                foreach (var id in outcome.ChangedAssetIds)
                    changed.Add(id);
            }
        }

        await PublishTickEnvelopeAsync(
            eventPublisher,
            EventTypes.SimulationTickCompleted,
            run.Id,
            nextTick,
            dueList,
            changed.ToList(),
            cancellationToken);

        await EmitTickEventsAsync(run.Id, nextTick, participating, assetRepo, notifier, cancellationToken);
    }

    private static async Task PublishTickEnvelopeAsync(
        IEventPublisher publisher,
        string eventType,
        string runId,
        int tickIndex,
        IReadOnlyList<string> dueAssetIds,
        IReadOnlyList<string>? changedAssetIds,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["tickIndex"] = tickIndex,
            ["dueAssetIds"] = dueAssetIds,
            ["engineCycleId"] = $"{runId}:{tickIndex}",
        };
        if (changedAssetIds != null)
            payload["changedAssetIds"] = changedAssetIds;

        var evt = new EventDto
        {
            AssetId = EventTypes.SimulationEngineAssetId,
            EventType = eventType,
            OccurredAt = DateTimeOffset.UtcNow,
            SimulationRunId = runId,
            RunTick = tickIndex,
            Payload = payload,
        };
        await publisher.PublishAsync(evt, cancellationToken);
    }

    private static async Task EmitTickEventsAsync(
        string runId,
        int tick,
        HashSet<string> participatingAssetIds,
        IAssetRepository assetRepo,
        ISimulationNotifier notifier,
        CancellationToken cancellationToken)
    {
        foreach (var assetId in participatingAssetIds)
        {
            var state = await assetRepo.GetStateByAssetIdAsync(assetId, cancellationToken);
            if (state == null) continue;

            var properties = state.Properties
                .Where(kvp =>
                    kvp.Value != null
                    && !SimulationAssetMetadataKeys.ShouldExcludeFromClientTickPayload(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

            var tickEvent = new SimulationTickEvent(
                RunId: runId,
                Tick: tick,
                AssetId: assetId,
                Properties: properties,
                Status: state.Status ?? "normal",
                Timestamp: DateTimeOffset.UtcNow);

            await notifier.NotifyAsync(tickEvent, cancellationToken);
        }
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
