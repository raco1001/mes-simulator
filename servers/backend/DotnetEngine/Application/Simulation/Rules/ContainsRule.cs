using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation.Constants;

namespace DotnetEngine.Application.Simulation.Rules;

/// <summary>
/// 관계 타입 "Contains": 컨테이너가 포함된 에셋에 상태(Status 등) 전파.
/// </summary>
public sealed class ContainsRule : IPropagationRule
{
    public bool CanApply(PropagationContext ctx) =>
        string.Equals(ctx.Relationship.RelationshipType, "Contains", StringComparison.OrdinalIgnoreCase);

    public PropagationResult Apply(PropagationContext ctx)
    {
        var incoming = ctx.IncomingPatch;
        var fromState = ctx.FromState;
        var outgoingPatch = new StatePatchDto
        {
            CurrentTemp = incoming.CurrentTemp,
            CurrentPower = incoming.CurrentPower,
            Status = incoming.Status ?? fromState?.Status ?? "normal",
            LastEventType = incoming.LastEventType ?? EventTypes.SimulationStateUpdated,
        };
        var occurredAt = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["tick"] = ctx.RunTick,
            ["depth"] = ctx.Depth,
            ["relationshipType"] = "Contains",
            ["fromAssetId"] = ctx.FromAssetId,
        };
        if (ctx.Relationship.Id != null)
            payload["relationshipId"] = ctx.Relationship.Id;
        var evt = new EventDto
        {
            AssetId = ctx.ToAssetId,
            EventType = EventTypes.SimulationStateUpdated,
            OccurredAt = occurredAt,
            SimulationRunId = ctx.SimulationRunId,
            RunTick = ctx.RunTick,
            RelationshipId = ctx.Relationship.Id,
            Payload = payload,
        };
        return new PropagationResult
        {
            OutgoingPatch = outgoingPatch,
            Events = [evt],
        };
    }
}
