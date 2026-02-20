using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driving;

namespace DotnetEngine.Application.Simulation.Handlers;

/// <summary>
/// 시뮬레이션 실행 Use Case 구현. 에셋·관계를 조회해 그래프를 구성하고 최소 1회 시뮬레이션 결과를 반환.
/// </summary>
public sealed class RunSimulationCommandHandler : IRunSimulationCommand
{
    private readonly IGetAssetsQuery _getAssetsQuery;
    private readonly IGetRelationshipsQuery _getRelationshipsQuery;

    public RunSimulationCommandHandler(
        IGetAssetsQuery getAssetsQuery,
        IGetRelationshipsQuery getRelationshipsQuery)
    {
        _getAssetsQuery = getAssetsQuery;
        _getRelationshipsQuery = getRelationshipsQuery;
    }

    public async Task<RunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _getAssetsQuery.GetAllAsync(cancellationToken);
        var relationships = await _getRelationshipsQuery.GetAllAsync(cancellationToken);

        // 메모리상 그래프 구성: 노드(에셋) 수, 엣지(관계) 수만 사용하는 최소 시나리오
        var assetsCount = assets.Count;
        var relationshipsCount = relationships.Count;

        return new RunResult
        {
            Success = true,
            Message = "Simulation run completed",
            AssetsCount = assetsCount,
            RelationshipsCount = relationshipsCount,
        };
    }
}
