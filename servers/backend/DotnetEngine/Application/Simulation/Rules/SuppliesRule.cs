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
            transferred = ApplyMappings(ctx.Relationship.Mappings, source);
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

    private static Dictionary<string, object?> ApplyMappings(
        IReadOnlyList<PropertyMapping> mappings,
        IReadOnlyDictionary<string, object?> source)
    {
        var result = new Dictionary<string, object?>();
        foreach (var m in mappings)
        {
            if (string.IsNullOrWhiteSpace(m.FromProperty) || string.IsNullOrWhiteSpace(m.ToProperty))
                continue;
            if (!source.TryGetValue(m.FromProperty, out var raw) || raw is null)
                continue;
            if (!TransferSpecParser.TryCoerceDouble(raw, out var value))
                continue;
            result[m.ToProperty] = ApplyTransform(m.TransformRule, value);
        }
        return result;
    }

    private static double ApplyTransform(string rule, double value)
    {
        if (string.IsNullOrWhiteSpace(rule) || rule.Trim().Equals("value", StringComparison.Ordinal))
            return value;

        var parts = rule.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3
            && parts[0].Equals("value", StringComparison.Ordinal)
            && double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var operand))
        {
            return parts[1] switch
            {
                "*" => value * operand,
                "/" => operand != 0 ? value / operand : value,
                "+" => value + operand,
                "-" => value - operand,
                _ => value
            };
        }
        return value;
    }
}
