namespace DotnetEngine.Application.LinkType.Dto;

/// <summary>
/// Optional whitelist: allowed (fromPropertyKey, toPropertyKey) pairs for a link type.
/// </summary>
public sealed record PropertyMappingPairHint(string FromPropertyKey, string ToPropertyKey);
