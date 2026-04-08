using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// 참여 에셋의 persisted <c>states</c> 삭제 (dryRun이면 생략).
/// </summary>
public static class SimulationPersistedStateReset
{
    public static async Task ApplyIfNeededAsync(
        RunSimulationRequest request,
        bool dryRun,
        IRelationshipRepository relationshipRepository,
        IAssetRepository assetRepository,
        CancellationToken cancellationToken)
    {
        if (!request.ResetState || dryRun)
            return;

        var seedIds = request.ResolveTriggerAssetIds();
        if (seedIds.Count == 0)
            return;

        var participating = await SimulationParticipation.GetParticipatingAssetIdsAsync(
            seedIds,
            relationshipRepository,
            cancellationToken);

        if (participating.Count == 0)
            return;

        await assetRepository.DeleteStatesByAssetIdsAsync(participating.ToList(), cancellationToken);
    }
}
