using DotnetEngine.Application.Relationship.Dto;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Domain.Simulation.Constants;

namespace DotnetEngine.Application.Simulation.Rules;

/// <summary>
/// 관계 타입 "Supplies": 온도/전력 등을 공급처에서 대상으로 전달.
/// </summary>
public sealed class SuppliesRule : IPropagationRule
{
    public bool CanApply(PropagationContext ctx) =>
        string.Equals(ctx.Relationship.RelationshipType, "Supplies", StringComparison.OrdinalIgnoreCase);

    public PropagationResult Apply(PropagationContext ctx)
    {
        var fromState = ctx.FromState;
        var incoming = ctx.IncomingPatch;
        var source = TransferSpecParser.ResolveSourceProperties(incoming, fromState);

        Dictionary<string, object?> transferred;
        if (ctx.Relationship.Mappings is { Count: > 0 })
            transferred = PropertyMappingPropagation.ApplyMappings(ctx.Relationship.Mappings, source);
        else
        {
            var transfers = TransferSpecParser.Parse(ctx.Relationship.Properties);
            transferred = TransferSpecParser.BuildTransferredProperties(transfers, incoming, fromState);
        }

        var outgoingPatch = new StatePatchDto
        {
            Properties = transferred,
            Status = incoming.Status ?? fromState?.Status,
            LastEventType = incoming.LastEventType ?? EventTypes.SimulationStateUpdated,
        };
        var occurredAt = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["tick"] = ctx.RunTick,
            ["depth"] = ctx.Depth,
            ["relationshipType"] = "Supplies",
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
