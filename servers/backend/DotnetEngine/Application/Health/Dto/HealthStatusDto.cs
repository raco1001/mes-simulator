namespace DotnetEngine.Application.Health.Dto;

/// <summary>
/// Health 상태 API 응답 DTO.
/// </summary>
public sealed record HealthStatusDto
{
    public required string Status { get; init; }
    public required string Description { get; init; }
    public required string ApplicationName { get; init; }
    public required DateTimeOffset ReportedAt { get; init; }
}
