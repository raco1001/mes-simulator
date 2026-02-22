using System.Net;
using System.Net.Http.Json;
using DotnetEngine.Application.Alert.Dto;
using DotnetEngine.Application.Alert.Ports.Driven;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DotnetEngine.Tests.Presentation;

public class AlertControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetAlerts_Returns200AndEmptyList_WhenNoAlerts()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAlertStore>();
                services.AddSingleton<IAlertStore>(new StubAlertStore(Array.Empty<AlertDto>()));
            });
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_Returns200AndAlertList_WhenStoreHasAlerts()
    {
        var now = DateTimeOffset.UtcNow;
        var alerts = new List<AlertDto>
        {
            new()
            {
                AssetId = "freezer-1",
                Timestamp = now,
                Severity = "warning",
                Message = "Asset state: warning",
                RunId = null,
                Metric = "temperature",
                Current = -5.0,
                Threshold = -10,
                Code = "TEMP_HIGH",
                Metadata = new Dictionary<string, object>(),
            },
        };
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAlertStore>();
                services.AddSingleton<IAlertStore>(new StubAlertStore(alerts));
            });
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("freezer-1", list[0].AssetId);
        Assert.Equal("warning", list[0].Severity);
        Assert.Equal("Asset state: warning", list[0].Message);
    }

    [Fact]
    public async Task GetAlerts_RespectsLimitQuery()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAlertStore>();
                services.AddSingleton<IAlertStore>(new StubAlertStore(Array.Empty<AlertDto>()));
            });
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/alerts?limit=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class StubAlertStore : IAlertStore
    {
        private readonly IReadOnlyList<AlertDto> _alerts;

        public StubAlertStore(IReadOnlyList<AlertDto> alerts) => _alerts = alerts;

        public void Add(AlertDto alert) { }

        public IReadOnlyList<AlertDto> GetLatest(int maxCount) =>
            _alerts.Take(maxCount).ToList();
    }
}
