using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// 트리거 에셋에서 나가는 관계만 따라 참여 에셋 집합을 구한다 (엔진·Run 스냅샷 공통).
/// </summary>
public static class SimulationParticipation
{
    public static async Task<HashSet<string>> GetParticipatingAssetIdsAsync(
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
}
