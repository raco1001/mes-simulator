namespace DotnetEngine.Domain.Health.Constants;

/// <summary>
/// Health 도메인에서 사용하는 상수.
/// </summary>
public static class HealthConstants
{
    public static class Status
    {
        public const string Healthy = "Healthy";
        public const string Degraded = "Degraded";
        public const string Unhealthy = "Unhealthy";
    }

    public static class Defaults
    {
        public const string ApplicationName = "dotnet-engine";
    }
}
