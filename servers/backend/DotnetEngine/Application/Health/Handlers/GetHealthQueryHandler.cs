using DotnetEngine.Application.Health.Dto;
using DotnetEngine.Application.Health.Ports;
using DotnetEngine.Domain.Health.Constants;
using DotnetEngine.Domain.Health.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace DotnetEngine.Application.Health.Handlers;

/// <summary>
/// Health 상태 조회 Use Case 구현 (Port 구현체).
/// </summary>
public sealed class GetHealthQueryHandler : IGetHealthQuery
{
    private readonly IConfiguration _configuration;

    public GetHealthQueryHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthStatusDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var appName = _configuration["Application:Name"] ?? HealthConstants.Defaults.ApplicationName;
        var report = new HealthReport(
            HealthStatusKind.Healthy,
            "Application is running.",
            appName,
            DateTimeOffset.UtcNow);

        var dto = new HealthStatusDto
        {
            Status = report.StatusName,
            Description = report.Description,
            ApplicationName = report.ApplicationName,
            ReportedAt = report.ReportedAt
        };

        return Task.FromResult(dto);
    }
}
