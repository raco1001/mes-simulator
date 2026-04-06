using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Domain.Simulation;
using DotnetEngine.Domain.Simulation.ValueObjects;
using DotnetEngine.Application.Relationship.Ports.Driven;
using System.Linq;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 지속 시뮬레이션 시작 Use Case. Run 생성(Status=Running), 참여 에셋 초기 스냅샷 저장. 전파는 호출하지 않음.
/// </summary>
public sealed class StartContinuousRunCommandHandler : IStartContinuousRunCommand
{
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IRelationshipRepository _relationshipRepository;

    public StartContinuousRunCommandHandler(
        ISimulationRunRepository simulationRunRepository,
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository)
    {
        _simulationRunRepository = simulationRunRepository;
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
    }

    public async Task<StartContinuousRunResult> StartAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var running = await _simulationRunRepository.GetRunningAsync(cancellationToken);
        if (running.Count >= 1)
            return new StartContinuousRunResult
            {
                Success = false,
                RunId = "",
                Message = "Another simulation run is already running. Stop it before starting a new one.",
            };

        var runId = Guid.NewGuid().ToString("N");
        var maxDepth = request.MaxDepth <= 0
            ? SimulationEngineConstants.DefaultLeafPropagationMaxDepth
            : request.MaxDepth;
        var engineTick = SimulationEngineConstants.ClampEngineTickIntervalMs(request.EngineTickIntervalMs);
        var startedAt = DateTimeOffset.UtcNow;
        var trigger = StatePatchToDictionary(request.Patch);

        var runDto = new SimulationRunDto
        {
            Id = runId,
            Status = SimulationRunStatus.Running,
            StartedAt = startedAt,
            EndedAt = null,
            TriggerAssetId = request.TriggerAssetId,
            Trigger = trigger,
            MaxDepth = maxDepth,
            EngineTickIntervalMs = engineTick,
            TickIndex = 0,
        };

        var participating = await SimulationParticipation.GetParticipatingAssetIdsAsync(
            request.TriggerAssetId,
            _relationshipRepository,
            cancellationToken);
        var snapshot = new Dictionary<string, object>();
        foreach (var assetId in participating)
        {
            var state = await _assetRepository.GetStateByAssetIdAsync(assetId, cancellationToken);
            if (state is null)
                continue;
            var props = state.Properties
                .Where(kv => kv.Value != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);
            snapshot[assetId] = new Dictionary<string, object>
            {
                ["properties"] = props,
                ["simulationStatus"] = state.SimulationStatus ?? state.Status,
            };
        }

        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);
        if (snapshot.Count > 0)
            await _simulationRunRepository.ReplaceInitialSnapshotAsync(runId, snapshot, cancellationToken);

        return new StartContinuousRunResult
        {
            Success = true,
            RunId = runId,
        };
    }

    private static IReadOnlyDictionary<string, object> StatePatchToDictionary(StatePatchDto? patch)
    {
        if (patch == null)
            return new Dictionary<string, object>();

        var d = new Dictionary<string, object>(patch.Properties
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value!));
        if (patch.Status != null)
            d["status"] = patch.Status;
        if (patch.LastEventType != null)
            d["lastEventType"] = patch.LastEventType;
        return d;
    }
}
