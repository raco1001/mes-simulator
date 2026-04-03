using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetEngine.Application.Recommendation.Dto;

public sealed record PipelineRecommendationDto
{
    [JsonPropertyName("recommendationId")]
    public required string RecommendationId { get; init; }
    [JsonPropertyName("objectId")]
    public required string ObjectId { get; init; }
    [JsonPropertyName("objectType")]
    public required string ObjectType { get; init; }
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }
    [JsonPropertyName("category")]
    public required string Category { get; init; }
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    [JsonPropertyName("suggestedAction")]
    public JsonElement SuggestedAction { get; init; }
    [JsonPropertyName("analysisBasis")]
    public JsonElement AnalysisBasis { get; init; }
    [JsonPropertyName("status")]
    public required string Status { get; init; }
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
