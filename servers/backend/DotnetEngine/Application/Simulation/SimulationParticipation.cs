using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Relationship.Ports.Driven;

namespace DotnetEngine.Application.Simulation;

/// <summary>
/// 트리거 에셋에서 나가는 관계만 따라 참여 에셋 집합을 구한 뒤,
/// 이미 참여한 노드로 들어오는 <c>Supplies</c> 공급처(상류)를 포함한다 (엔진·Run 스냅샷 공통).
/// 드론·게이트웨이만 트리거로 두어도 Region/Center 같은 전력 공급 에셋이 tick·전파 대상이 된다.
/// </summary>
public static class SimulationParticipation
{
    /// <summary>단일 시드; 다중 시드는 <see cref="GetParticipatingAssetIdsAsync(System.Collections.Generic.IReadOnlyList{string},IRelationshipRepository,System.Threading.CancellationToken)"/>.</summary>
    public static Task<HashSet<string>> GetParticipatingAssetIdsAsync(
        string triggerAssetId,
        IRelationshipRepository relRepo,
        CancellationToken cancellationToken) =>
        GetParticipatingAssetIdsAsync(
            string.IsNullOrWhiteSpace(triggerAssetId)
                ? Array.Empty<string>()
                : new[] { triggerAssetId.Trim() },
            relRepo,
            cancellationToken);

    /// <summary>여러 시드를 큐에 넣고 동일 BFS(나가는 관계만)로 합집합을 구한다.</summary>
    public static async Task<HashSet<string>> GetParticipatingAssetIdsAsync(
        IReadOnlyList<string> seedAssetIds,
        IRelationshipRepository relRepo,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        var seedDedup = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in seedAssetIds ?? Array.Empty<string>())
        {
            var id = raw?.Trim();
            if (string.IsNullOrEmpty(id) || !seedDedup.Add(id))
                continue;
            queue.Enqueue(id);
        }

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

        await AddUpstreamSuppliesSourcesAsync(visited, relRepo, cancellationToken);
        return visited;
    }

    /// <summary>
    /// Reverse walk on Supplies: any asset that supplies a participating node must participate so the engine can fire propagation from it.
    /// </summary>
    private static async Task AddUpstreamSuppliesSourcesAsync(
        HashSet<string> participating,
        IRelationshipRepository relRepo,
        CancellationToken cancellationToken)
    {
        var all = await relRepo.GetAllAsync(cancellationToken) ?? Array.Empty<RelationshipDto>();
        var suppliesSourcesByTarget = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var rel in all)
        {
            if (!string.Equals(rel.RelationshipType, "Supplies", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!suppliesSourcesByTarget.TryGetValue(rel.ToAssetId, out var list))
            {
                list = new List<string>();
                suppliesSourcesByTarget[rel.ToAssetId] = list;
            }
            list.Add(rel.FromAssetId);
        }

        var q = new Queue<string>();
        foreach (var id in participating)
            q.Enqueue(id);

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (!suppliesSourcesByTarget.TryGetValue(t, out var sources))
                continue;
            foreach (var s in sources)
            {
                if (participating.Add(s))
                    q.Enqueue(s);
            }
        }
    }
}
