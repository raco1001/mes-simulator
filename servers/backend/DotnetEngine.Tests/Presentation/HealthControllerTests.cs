using System.Net;
using System.Net.Http.Json;
using DotnetEngine.Application.Health.Dto;
using DotnetEngine.Application.Health.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DotnetEngine.Tests.Presentation;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGetHealthQuery>();
                services.AddScoped<IGetHealthQuery, DotnetEngine.Application.Health.Handlers.GetHealthQueryHandler>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Returns200AndHealthStatusDto()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        Assert.NotNull(dto);
        Assert.Equal("Healthy", dto.Status);
        Assert.Equal("dotnet-engine", dto.ApplicationName);
        Assert.NotNull(dto.Description);
        Assert.True(dto.ReportedAt != default);
    }
}
