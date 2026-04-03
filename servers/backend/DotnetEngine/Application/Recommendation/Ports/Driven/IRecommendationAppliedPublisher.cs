namespace DotnetEngine.Application.Recommendation.Ports.Driven;

public interface IRecommendationAppliedPublisher
{
    Task PublishAsync(
        string recommendationId,
        string triggerAssetId,
        IReadOnlyDictionary<string, object?> patch,
        string runId,
        DateTimeOffset appliedAt,
        CancellationToken cancellationToken = default);
}
