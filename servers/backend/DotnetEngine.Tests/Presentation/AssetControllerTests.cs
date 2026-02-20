using System.Net;
using System.Net.Http.Json;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Presentation;

public class AssetControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AssetControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                services.RemoveAll<IGetAssetsQuery>();
                services.RemoveAll<IGetStatesQuery>();
                services.RemoveAll<ICreateAssetCommand>();
                services.RemoveAll<IUpdateAssetCommand>();

                // Mock repositories
                var mockAssetRepository = new Mock<IAssetRepository>();
                var now = DateTimeOffset.UtcNow;
                var assets = new List<AssetDto>
                {
                    new()
                    {
                        Id = "freezer-1",
                        Type = "freezer",
                        Connections = new List<string> { "conveyor-1" },
                        Metadata = new Dictionary<string, object>(),
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                };
                mockAssetRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets);
                mockAssetRepository.Setup(r => r.GetByIdAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets[0]);
                mockAssetRepository.Setup(r => r.GetByIdAsync("not-found", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AssetDto?)null);

                mockAssetRepository.Setup(r => r.AddAsync(It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AssetDto a, CancellationToken _) => a);
                var updatedAsset = new AssetDto
                {
                    Id = "freezer-1",
                    Type = "conveyor",
                    Connections = new List<string>(),
                    Metadata = new Dictionary<string, object>(),
                    CreatedAt = now,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                mockAssetRepository.Setup(r => r.UpdateAsync("freezer-1", It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedAsset);
                mockAssetRepository.Setup(r => r.UpdateAsync("not-found", It.IsAny<AssetDto>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AssetDto?)null);

                var states = new List<StateDto>
                {
                    new()
                    {
                        AssetId = "freezer-1",
                        CurrentTemp = -5.0,
                        CurrentPower = 120.0,
                        Status = "normal",
                        LastEventType = "asset.health.updated",
                        UpdatedAt = now,
                        Metadata = new Dictionary<string, object>()
                    }
                };
                mockAssetRepository.Setup(r => r.GetAllStatesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(states);
                mockAssetRepository.Setup(r => r.GetStateByAssetIdAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(states[0]);
                mockAssetRepository.Setup(r => r.GetStateByAssetIdAsync("not-found", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StateDto?)null);

                services.AddSingleton(mockAssetRepository.Object);
                services.AddScoped<IGetAssetsQuery, DotnetEngine.Application.Asset.Handlers.GetAssetsQueryHandler>();
                services.AddScoped<IGetStatesQuery, DotnetEngine.Application.Asset.Handlers.GetStatesQueryHandler>();
                services.AddScoped<ICreateAssetCommand, DotnetEngine.Application.Asset.Handlers.CreateAssetCommandHandler>();
                services.AddScoped<IUpdateAssetCommand, DotnetEngine.Application.Asset.Handlers.UpdateAssetCommandHandler>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetAssets_Returns200AndListOfAssets()
    {
        var response = await _client.GetAsync("/api/assets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<AssetDto>>();
        Assert.NotNull(dtos);
        Assert.Single(dtos);
        Assert.Equal("freezer-1", dtos[0].Id);
    }

    [Fact]
    public async Task GetAssetById_Returns200AndAsset_WhenExists()
    {
        var response = await _client.GetAsync("/api/assets/freezer-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(dto);
        Assert.Equal("freezer-1", dto.Id);
        Assert.Equal("freezer", dto.Type);
    }

    [Fact]
    public async Task GetAssetById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/assets/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStates_Returns200AndListOfStates()
    {
        var response = await _client.GetAsync("/api/states");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<StateDto>>();
        Assert.NotNull(dtos);
        Assert.Single(dtos);
        Assert.Equal("freezer-1", dtos[0].AssetId);
    }

    [Fact]
    public async Task GetStateByAssetId_Returns200AndState_WhenExists()
    {
        var response = await _client.GetAsync("/api/states/freezer-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StateDto>();
        Assert.NotNull(dto);
        Assert.Equal("freezer-1", dto.AssetId);
        Assert.Equal("normal", dto.Status);
    }

    [Fact]
    public async Task GetStateByAssetId_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/states/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAssets_Returns201AndAsset()
    {
        var request = new CreateAssetRequest
        {
            Type = "sensor",
            Connections = Array.Empty<string>(),
            Metadata = new Dictionary<string, object>()
        };
        var response = await _client.PostAsJsonAsync("/api/assets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(dto);
        Assert.Equal("sensor", dto.Type);
        Assert.NotNull(dto.Id);
    }

    [Fact]
    public async Task PutAsset_Returns200AndAsset_WhenExists()
    {
        var request = new UpdateAssetRequest { Type = "conveyor", Connections = Array.Empty<string>(), Metadata = null };
        var response = await _client.PutAsJsonAsync("/api/assets/freezer-1", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(dto);
        Assert.Equal("freezer-1", dto.Id);
        Assert.Equal("conveyor", dto.Type);
    }

    [Fact]
    public async Task PutAsset_Returns404_WhenNotFound()
    {
        var request = new UpdateAssetRequest { Type = "conveyor" };
        var response = await _client.PutAsJsonAsync("/api/assets/not-found", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
