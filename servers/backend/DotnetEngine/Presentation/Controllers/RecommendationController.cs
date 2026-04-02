using System.Text.Json;
using System.Linq;
using DotnetEngine.Application.Recommendation.Ports.Driven;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEngine.Presentation.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationController : ControllerBase
{
    private readonly IPipelineRecommendationClient _recommendationClient;
    private readonly IRunSimulationCommand _runSimulationCommand;
    private readonly IRecommendationAppliedPublisher _appliedPublisher;

    public RecommendationController(
        IPipelineRecommendationClient recommendationClient,
        IRunSimulationCommand runSimulationCommand,
        IRecommendationAppliedPublisher appliedPublisher)
    {
        _recommendationClient = recommendationClient;
        _runSimulationCommand = runSimulationCommand;
        _appliedPublisher = appliedPublisher;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? severity, CancellationToken cancellationToken)
    {
        var items = await _recommendationClient.ListAsync(status, severity, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{recommendationId}")]
    public async Task<IActionResult> GetById(string recommendationId, CancellationToken cancellationToken)
    {
        var item = await _recommendationClient.GetByIdAsync(recommendationId, cancellationToken);
        if (item is null)
            return NotFound();
        return Ok(item);
    }

    [HttpPatch("{recommendationId}")]
    public async Task<IActionResult> UpdateStatus(string recommendationId, [FromBody] RecommendationStatusPatch patch, CancellationToken cancellationToken)
    {
        var item = await _recommendationClient.UpdateStatusAsync(recommendationId, patch.Status, cancellationToken);
        if (item is null)
            return NotFound();
        return Ok(item);
    }

    [HttpPost("{recommendationId}/apply")]
    public async Task<IActionResult> Apply(string recommendationId, CancellationToken cancellationToken)
    {
        var recommendation = await _recommendationClient.GetByIdAsync(recommendationId, cancellationToken);
        if (recommendation is null)
            return NotFound(new { error = "Recommendation not found" });

        var suggestedAction = recommendation.SuggestedAction;
        if (!suggestedAction.TryGetProperty("triggerAssetId", out var triggerAssetIdNode) || triggerAssetIdNode.ValueKind != JsonValueKind.String)
            return BadRequest(new { error = "suggestedAction.triggerAssetId is required" });
        var triggerAssetId = triggerAssetIdNode.GetString()!;

        var patchProperties = new Dictionary<string, object?>();
        if (suggestedAction.TryGetProperty("patch", out var patchNode)
            && patchNode.ValueKind == JsonValueKind.Object
            && patchNode.TryGetProperty("properties", out var propertiesNode)
            && propertiesNode.ValueKind == JsonValueKind.Object)
        {
            patchProperties = JsonToDictionary(propertiesNode);
        }

        await _recommendationClient.UpdateStatusAsync(recommendationId, "approved", cancellationToken);

        var runResult = await _runSimulationCommand.RunAsync(new RunSimulationRequest
        {
            TriggerAssetId = triggerAssetId,
            Patch = new StatePatchDto { Properties = patchProperties },
            MaxDepth = 3,
            RunTick = 0
        }, cancellationToken);

        var updated = await _recommendationClient.UpdateStatusAsync(recommendationId, "applied", cancellationToken);
        var appliedAt = DateTimeOffset.UtcNow;
        await _appliedPublisher.PublishAsync(
            recommendationId,
            triggerAssetId,
            patchProperties,
            runResult.RunId,
            appliedAt,
            cancellationToken);

        return Ok(new
        {
            success = true,
            runId = runResult.RunId,
            recommendation = updated
        });
    }

    public sealed record RecommendationStatusPatch
    {
        public required string Status { get; init; }
    }

    private static Dictionary<string, object?> JsonToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in element.EnumerateObject())
            dict[prop.Name] = JsonToValue(prop.Value);
        return dict;
    }

    private static object? JsonToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i) => i,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => JsonToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonToValue).ToList(),
            _ => null
        };
    }
}
