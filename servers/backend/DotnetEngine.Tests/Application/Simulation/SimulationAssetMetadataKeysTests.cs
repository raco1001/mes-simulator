using DotnetEngine.Application.Simulation;
using Xunit;

namespace DotnetEngine.Tests.Application.Simulation;

public class SimulationAssetMetadataKeysTests
{
    [Theory]
    [InlineData("canvasPosition", true)]
    [InlineData("CanvasPosition", true)]
    [InlineData("canvas_position", true)]
    [InlineData("power", false)]
    public void IsReservedForPropertyOverlay_CanvasPositionSkipped(string key, bool reserved) =>
        Assert.Equal(reserved, SimulationAssetMetadataKeys.IsReservedForPropertyOverlay(key));

    [Theory]
    [InlineData("assetName", true)]
    [InlineData("asset_name", true)]
    [InlineData("streamOut", false)]
    public void ShouldExcludeFromClientTickPayload_UiOnlyKeys(string key, bool exclude) =>
        Assert.Equal(exclude, SimulationAssetMetadataKeys.ShouldExcludeFromClientTickPayload(key));
}
