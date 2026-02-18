using System.Net;
using System.Net.Http.Json;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports;
using DotnetEngine.Domain.Asset.ValueObjects;
using DotnetEngine.Infrastructure.Mongo;
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

                // Mock repositories
                var mockAssetRepository = new Mock<IAssetRepository>();
                var assets = new List<Asset>
                {
                    new Asset
                    {
                        Id = "freezer-1",
                        Type = "freezer",
                        Connections = new List<string> { "conveyor-1" },
                        Metadata = new Dictionary<string, object>(),
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    }
                };
                mockAssetRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets);
                mockAssetRepository.Setup(r => r.GetByIdAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets[0]);
                mockAssetRepository.Setup(r => r.GetByIdAsync("not-found", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Asset?)null);

                var states = new List<AssetState>
                {
                    new AssetState
                    {
                        AssetId = "freezer-1",
                        CurrentTemp = -5.0,
                        CurrentPower = 120.0,
                        Status = "normal",
                        LastEventType = "asset.health.updated",
                        UpdatedAt = DateTimeOffset.UtcNow,
                        Metadata = new Dictionary<string, object>()
                    }
                };
                mockAssetRepository.Setup(r => r.GetAllStatesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(states);
                mockAssetRepository.Setup(r => r.GetStateByAssetIdAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(states[0]);
                mockAssetRepository.Setup(r => r.GetStateByAssetIdAsync("not-found", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AssetState?)null);

                services.AddSingleton(mockAssetRepository.Object);
                services.AddScoped<IGetAssetsQuery, DotnetEngine.Application.Asset.Handlers.GetAssetsQueryHandler>();
                services.AddScoped<IGetStatesQuery, DotnetEngine.Application.Asset.Handlers.GetStatesQueryHandler>();
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
}
