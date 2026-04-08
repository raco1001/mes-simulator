using System.Text.Json;

namespace DotnetEngine.Presentation.Controllers;

/// <summary>POST /api/simulation/runs/{runId}/overrides 요청 본문.</summary>
public sealed class AppendSimulationOverrideRequestBody
{
    public required string AssetId { get; init; }
    public required string PropertyKey { get; init; }
    public JsonElement Value { get; init; }
    public required int FromTick { get; init; }
    public int? ToTick { get; init; }
}
