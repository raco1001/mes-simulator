using Xunit;
using DomainAsset = DotnetEngine.Domain.Asset.Entities.Asset;

namespace DotnetEngine.Tests.Domain.Asset.Entities;

public class AssetTests
{
    [Fact]
    public void Create_SetsIdTypeConnectionsMetadataAndTimestamps()
    {
        var connections = new List<string> { "c1" };
        var metadata = new Dictionary<string, object> { ["k"] = "v" };
        var before = DateTimeOffset.UtcNow;

        var asset = DomainAsset.Create("asset-1", "freezer", connections, metadata);

        var after = DateTimeOffset.UtcNow;
        Assert.Equal("asset-1", asset.Id);
        Assert.Equal("freezer", asset.Type);
        Assert.Equal(connections, asset.Connections);
        Assert.Equal(metadata, asset.Metadata);
        Assert.True(asset.CreatedAt >= before && asset.CreatedAt <= after);
        Assert.True(asset.UpdatedAt >= before && asset.UpdatedAt <= after);
    }

    [Fact]
    public void Create_WithNullConnections_DefaultsToEmpty()
    {
        var asset = DomainAsset.Create("id", "type", null, null);
        Assert.NotNull(asset.Connections);
        Assert.Empty(asset.Connections);
    }

    [Fact]
    public void Create_Throws_WhenIdNull()
    {
        Assert.Throws<ArgumentNullException>(() => DomainAsset.Create(null!, "type", null, null));
    }

    [Fact]
    public void Create_Throws_WhenTypeNull()
    {
        Assert.Throws<ArgumentNullException>(() => DomainAsset.Create("id", null!, null, null));
    }

    [Fact]
    public void Restore_SetsAllProperties()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;
        var asset = DomainAsset.Restore("r1", "sensor", new List<string>(), new Dictionary<string, object>(), createdAt, updatedAt);

        Assert.Equal("r1", asset.Id);
        Assert.Equal("sensor", asset.Type);
        Assert.Equal(createdAt, asset.CreatedAt);
        Assert.Equal(updatedAt, asset.UpdatedAt);
    }

    [Fact]
    public void UpdateType_UpdatesTypeAndTouchUpdatedAt()
    {
        var asset = DomainAsset.Create("id", "freezer", null, null);
        var prev = asset.UpdatedAt;

        asset.UpdateType("conveyor");

        Assert.Equal("conveyor", asset.Type);
        Assert.True(asset.UpdatedAt >= prev);
    }

    [Fact]
    public void UpdateConnections_UpdatesConnectionsAndTouchUpdatedAt()
    {
        var asset = DomainAsset.Create("id", "type", new List<string>(), null);
        var newConnections = new List<string> { "a", "b" };

        asset.UpdateConnections(newConnections);

        Assert.Equal(newConnections, asset.Connections);
    }

    [Fact]
    public void UpdateMetadata_UpdatesMetadataAndTouchUpdatedAt()
    {
        var asset = DomainAsset.Create("id", "type", null, new Dictionary<string, object>());
        var newMeta = new Dictionary<string, object> { ["x"] = 1 };

        asset.UpdateMetadata(newMeta);

        Assert.Equal(newMeta, asset.Metadata);
    }

    [Fact]
    public void TouchUpdatedAt_UpdatesUpdatedAt()
    {
        var asset = DomainAsset.Create("id", "type", null, null);
        var prev = asset.UpdatedAt;

        asset.TouchUpdatedAt();

        Assert.True(asset.UpdatedAt >= prev);
    }
}
