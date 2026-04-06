using DotnetEngine.Application.Asset.Dto;

namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// RunOnePropagationAsync 결과: 병합 상태 + 상태가 실제로 바뀐 에셋 ID (Kafka 틱 봉투용).
/// </summary>
public sealed record RunPropagationOutcome(
    IReadOnlyDictionary<string, StateDto> States,
    IReadOnlyList<string> ChangedAssetIds);
