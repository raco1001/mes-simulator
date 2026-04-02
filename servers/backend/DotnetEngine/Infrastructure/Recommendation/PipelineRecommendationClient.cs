using System.Text;
using System.Text.Json;
using DotnetEngine.Application.Recommendation.Dto;
using DotnetEngine.Application.Recommendation.Ports.Driven;

namespace DotnetEngine.Infrastructure.Recommendation;

public sealed class PipelineRecommendationClient : IPipelineRecommendationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;

    public PipelineRecommendationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PipelineRecommendationDto>> ListAsync(string? status, string? severity, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(severity))
            query.Add($"severity={Uri.EscapeDataString(severity)}");
        var path = "/recommendations";
        if (query.Count > 0)
            path += $"?{string.Join("&", query)}";

        var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<PipelineRecommendationDto>>(body, JsonOptions) ?? [];
    }

    public async Task<PipelineRecommendationDto?> GetByIdAsync(string recommendationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/recommendations/{Uri.EscapeDataString(recommendationId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<PipelineRecommendationDto>(body, JsonOptions);
    }

    public async Task<PipelineRecommendationDto?> UpdateStatusAsync(string recommendationId, string status, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new { status }, JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"/recommendations/{Uri.EscapeDataString(recommendationId)}", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<PipelineRecommendationDto>(body, JsonOptions);
    }
}
