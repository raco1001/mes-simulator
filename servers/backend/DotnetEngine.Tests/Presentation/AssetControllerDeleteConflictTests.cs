using System.Net;
using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.ObjectType.Dto;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.Relationship.Ports.Driven;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Presentation;

/// <summary>
/// DELETE /api/assets/{id} returns 409 when relationships reference the asset.
/// </summary>
public sealed class AssetControllerDeleteConflictTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AssetControllerDeleteConflictTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGetAssetsQuery>();
                services.RemoveAll<IGetStatesQuery>();
                services.RemoveAll<ICreateAssetCommand>();
                services.RemoveAll<IUpdateAssetCommand>();
                services.RemoveAll<IDeleteAssetCommand>();
                services.RemoveAll<IObjectTypeSchemaRepository>();
                services.RemoveAll<IRelationshipRepository>();

                var now = DateTimeOffset.UtcNow;
                var assets = new List<AssetDto>
                {
                    new()
                    {
                        Id = "freezer-1",
                        Type = "freezer",
                        Connections = new List<string>(),
                        Metadata = new Dictionary<string, object>(),
                        CreatedAt = now,
                        UpdatedAt = now,
                    },
                };
                var mockAssetRepository = new Mock<IAssetRepository>();
                mockAssetRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets);
                mockAssetRepository.Setup(r => r.GetByIdAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets[0]);
                mockAssetRepository.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                var mockObjectTypeRepository = new Mock<IObjectTypeSchemaRepository>();
                mockObjectTypeRepository.Setup(r => r.GetByObjectTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ObjectTypeSchemaDto?)null);

                var mockRelationshipRepository = new Mock<IRelationshipRepository>();
                mockRelationshipRepository
                    .Setup(r => r.ExistsForAssetAsync("freezer-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                services.AddSingleton(mockAssetRepository.Object);
                services.AddSingleton(mockObjectTypeRepository.Object);
                services.AddSingleton(mockRelationshipRepository.Object);
                services.AddScoped<IGetAssetsQuery, DotnetEngine.Application.Asset.Handlers.GetAssetsQueryHandler>();
                services.AddScoped<IGetStatesQuery, DotnetEngine.Application.Asset.Handlers.GetStatesQueryHandler>();
                services.AddScoped<ICreateAssetCommand, DotnetEngine.Application.Asset.Handlers.CreateAssetCommandHandler>();
                services.AddScoped<IUpdateAssetCommand, DotnetEngine.Application.Asset.Handlers.UpdateAssetCommandHandler>();
                services.AddScoped<IDeleteAssetCommand, DotnetEngine.Application.Asset.Handlers.DeleteAssetCommandHandler>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task DeleteAsset_Returns409_WhenRelationshipsExist()
    {
        var response = await _client.DeleteAsync("/api/assets/freezer-1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
