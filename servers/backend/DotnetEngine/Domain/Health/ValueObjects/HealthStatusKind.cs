namespace DotnetEngine.Domain.Health.ValueObjects;

/// <summary>
/// Health 상태 종류 (ASP.NET Core HealthChecks와 정렬).
/// </summary>
public enum HealthStatusKind
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2
}
