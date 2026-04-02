using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Dto;
using DotnetEngine.Application.Simulation.Ports.Driven;
using Moq;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class EngineStateApplierTests
{
    [Fact]
    public async Task ApplyAsync_DryRunTrue_SkipsPersistenceAndPublish()
    {
        var assetRepo = new Mock<IAssetRepository>();
        var eventRepo = new Mock<IEventRepository>();
        var publisher = new Mock<IEventPublisher>();

        var sut = new EngineStateApplier(assetRepo.Object, eventRepo.Object, publisher.Object);

        await sut.ApplyAsync(
            new EventDto { AssetId = "a1", EventType = "simulation.state.updated", OccurredAt = DateTimeOffset.UtcNow, Payload = new Dictionary<string, object>() },
            new StateDto { AssetId = "a1", Status = "normal", UpdatedAt = DateTimeOffset.UtcNow },
            dryRun: true);

        assetRepo.Verify(x => x.UpsertStateAsync(It.IsAny<StateDto>(), It.IsAny<CancellationToken>()), Times.Never);
        eventRepo.Verify(x => x.AppendAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAsync(It.IsAny<EventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
