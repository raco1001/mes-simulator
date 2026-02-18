using DotnetEngine.Domain.Health.Constants;

namespace DotnetEngine.Domain.Health.ValueObjects;

/// <summary>
/// Health 검사 결과를 나타내는 Value Object.
/// </summary>
public sealed record HealthReport
{
    public HealthStatusKind Status { get; }
    public string Description { get; }
    public string ApplicationName { get; }
    public DateTimeOffset ReportedAt { get; }

    public HealthReport(
        HealthStatusKind status,
        string description,
        string applicationName,
        DateTimeOffset reportedAt)
    {
        Status = status;
        Description = description ?? string.Empty;
        ApplicationName = string.IsNullOrWhiteSpace(applicationName)
            ? HealthConstants.Defaults.ApplicationName
            : applicationName.Trim();
        ReportedAt = reportedAt;
    }

    public string StatusName => Status switch
    {
        HealthStatusKind.Healthy => HealthConstants.Status.Healthy,
        HealthStatusKind.Degraded => HealthConstants.Status.Degraded,
        HealthStatusKind.Unhealthy => HealthConstants.Status.Unhealthy,
        _ => HealthConstants.Status.Unhealthy
    };
}
