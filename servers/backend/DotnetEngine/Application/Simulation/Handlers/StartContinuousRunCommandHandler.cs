using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation;
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
    private readonly IRunSimulationCommand _runSimulationCommand;

    public StartContinuousRunCommandHandler(
        ISimulationRunRepository simulationRunRepository,
        IAssetRepository assetRepository,
        IRelationshipRepository relationshipRepository,
        IRunSimulationCommand runSimulationCommand)
    {
        _simulationRunRepository = simulationRunRepository;
        _assetRepository = assetRepository;
        _relationshipRepository = relationshipRepository;
        _runSimulationCommand = runSimulationCommand;
    }

    public async Task<StartContinuousRunResult> StartAsync(RunSimulationRequest request, CancellationToken cancellationToken = default)
    {
        var triggerIds = request.ResolveTriggerAssetIds();
        if (triggerIds.Count == 0)
            return new StartContinuousRunResult
            {
                Success = false,
                RunId = "",
                Message = "triggerAssetIds (non-empty) or triggerAssetId is required.",
            };
        if (RunSimulationRequestExtensions.IsMultiSeedPatchDisallowed(triggerIds, request.Patch))
            return new StartContinuousRunResult
            {
                Success = false,
                RunId = "",
                Message = "Multiple trigger seeds cannot be combined with a non-empty patch.",
            };

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
            TriggerAssetIds = triggerIds.ToList(),
            Trigger = trigger,
            MaxDepth = maxDepth,
            EngineTickIntervalMs = engineTick,
            TickIndex = 0,
        };

        var participating = await SimulationParticipation.GetParticipatingAssetIdsAsync(
            triggerIds,
            _relationshipRepository,
            cancellationToken);

        await SimulationPersistedStateReset.ApplyIfNeededAsync(
            request,
            dryRun: false,
            _relationshipRepository,
            _assetRepository,
            cancellationToken);

        IReadOnlyDictionary<string, object> snapshotRaw;
        if (request.ResetState)
        {
            snapshotRaw = await _runSimulationCommand.BuildBaselineInitialSnapshotAsync(
                participating,
                cancellationToken);
        }
        else
        {
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
            snapshotRaw = snapshot;
        }

        await _simulationRunRepository.CreateAsync(runDto, cancellationToken);
        if (snapshotRaw.Count > 0)
            await _simulationRunRepository.ReplaceInitialSnapshotAsync(runId, snapshotRaw, cancellationToken);

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
