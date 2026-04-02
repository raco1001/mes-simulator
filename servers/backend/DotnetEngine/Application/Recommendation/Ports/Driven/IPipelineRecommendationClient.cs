using DotnetEngine.Application.Recommendation.Dto;

namespace DotnetEngine.Application.Recommendation.Ports.Driven;

public interface IPipelineRecommendationClient
{
    Task<IReadOnlyList<PipelineRecommendationDto>> ListAsync(string? status, string? severity, CancellationToken cancellationToken = default);
    Task<PipelineRecommendationDto?> GetByIdAsync(string recommendationId, CancellationToken cancellationToken = default);
    Task<PipelineRecommendationDto?> UpdateStatusAsync(string recommendationId, string status, CancellationToken cancellationToken = default);
}
