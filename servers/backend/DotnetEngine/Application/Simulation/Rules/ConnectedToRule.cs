using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Rules;

/// <summary>
/// 관계 타입 "ConnectedTo": 연결된 에셋에 패치 제한 전파 (패치 그대로 전달).
/// </summary>
public sealed class ConnectedToRule : IPropagationRule
{
    private const string EventType = "simulation.state.updated";

    public bool CanApply(PropagationContext ctx) =>
        string.Equals(ctx.Relationship.RelationshipType, "ConnectedTo", StringComparison.OrdinalIgnoreCase);

    public PropagationResult Apply(PropagationContext ctx)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["depth"] = ctx.Depth,
            ["relationshipType"] = "ConnectedTo",
            ["fromAssetId"] = ctx.FromAssetId,
        };
        if (ctx.Relationship.Id != null)
            payload["relationshipId"] = ctx.Relationship.Id;
        var evt = new EventDto
        {
            AssetId = ctx.ToAssetId,
            EventType = EventType,
            OccurredAt = occurredAt,
            SimulationRunId = ctx.SimulationRunId,
            RelationshipId = ctx.Relationship.Id,
            Payload = payload,
        };
        return new PropagationResult
        {
            OutgoingPatch = ctx.IncomingPatch,
            Events = [evt],
        };
    }
}
