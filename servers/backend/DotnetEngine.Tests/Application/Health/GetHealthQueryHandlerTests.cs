using DotnetEngine.Application.Health.Dto;
using DotnetEngine.Application.Health.Handlers;
using DotnetEngine.Domain.Health.Constants;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DotnetEngine.Tests.Application.Health;

public class GetHealthQueryHandlerTests
{
    private static IConfiguration CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    [Fact]
    public async Task GetAsync_ReturnsHealthyStatusDto()
    {
        var handler = new GetHealthQueryHandler(CreateEmptyConfiguration());
        var result = await handler.GetAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthConstants.Status.Healthy, result.Status);
        Assert.Equal(HealthConstants.Defaults.ApplicationName, result.ApplicationName);
        Assert.NotNull(result.Description);
        Assert.True(result.ReportedAt <= DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.True(result.ReportedAt >= DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetAsync_ReturnsInstanceOfHealthStatusDto()
    {
        var handler = new GetHealthQueryHandler(CreateEmptyConfiguration());
        var result = await handler.GetAsync(CancellationToken.None);

        Assert.IsType<HealthStatusDto>(result);
    }
}
