using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

/// <summary>
/// 시뮬레이션 실행 Command Port (Primary Port).
/// </summary>
public interface IRunSimulationCommand
{
    Task<RunResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 주어진 Run에 대해 BFS 전파 1회만 수행. Run 생성/종료는 하지 않음. Run은 호출 전에 이미 존재한다고 가정.
    /// </summary>
    Task<RunPropagationOutcome> RunOnePropagationAsync(
        string runId,
        RunSimulationRequest request,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persisted state 없이 에셋·스키마만으로 베이스라인 스냅샷(dict: assetId → { properties, simulationStatus })을 만든다.
    /// 연속 런 <c>initialSnapshot</c> 저장용.
    /// </summary>
    Task<IReadOnlyDictionary<string, object>> BuildBaselineInitialSnapshotAsync(
        IReadOnlyCollection<string> participatingAssetIds,
        CancellationToken cancellationToken = default);
}
