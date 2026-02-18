using DotnetEngine.Domain.Health.Constants;
using DotnetEngine.Domain.Health.ValueObjects;
using Xunit;

namespace DotnetEngine.Tests.Domain.Health.ValueObjects;

public class HealthReportTests
{
    [Fact]
    public void Ctor_SetsProperties_AndStatusNameMatchesConstants()
    {
        var reportedAt = new DateTimeOffset(2025, 2, 17, 12, 0, 0, TimeSpan.Zero);
        var report = new HealthReport(
            HealthStatusKind.Healthy,
            "OK",
            "my-app",
            reportedAt);

        Assert.Equal(HealthStatusKind.Healthy, report.Status);
        Assert.Equal("OK", report.Description);
        Assert.Equal("my-app", report.ApplicationName);
        Assert.Equal(reportedAt, report.ReportedAt);
        Assert.Equal(HealthConstants.Status.Healthy, report.StatusName);
    }

    [Fact]
    public void Ctor_WhenApplicationNameNullOrWhiteSpace_UsesDefault()
    {
        var report = new HealthReport(
            HealthStatusKind.Healthy,
            "",
            "",
            DateTimeOffset.UtcNow);

        Assert.Equal(HealthConstants.Defaults.ApplicationName, report.ApplicationName);
    }

    [Theory]
    [InlineData(HealthStatusKind.Healthy, HealthConstants.Status.Healthy)]
    [InlineData(HealthStatusKind.Degraded, HealthConstants.Status.Degraded)]
    [InlineData(HealthStatusKind.Unhealthy, HealthConstants.Status.Unhealthy)]
    public void StatusName_ReturnsCorrectConstant(HealthStatusKind kind, string expectedName)
    {
        var report = new HealthReport(kind, "desc", "app", DateTimeOffset.UtcNow);
        Assert.Equal(expectedName, report.StatusName);
    }
}
