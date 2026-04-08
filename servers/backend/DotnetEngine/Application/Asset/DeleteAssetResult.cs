namespace DotnetEngine.Application.Asset;

/// <summary>
/// Result of attempting to delete an asset (driving port outcome).
/// </summary>
public enum DeleteAssetResult
{
    NotFound,
    HasRelationships,
    Deleted,
}
