using DotnetEngine.Domain.Simulation;
using DotnetEngine.Domain.Simulation.ValueObjects;

namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 런 세션 DTO (트리거 + 1회 전파 실행 단위).
/// </summary>
public sealed record SimulationRunDto
{
    public required string Id { get; init; }
    public required SimulationRunStatus Status { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    /// <summary>All seed asset ids (deduped, order preserved).</summary>
    public required IReadOnlyList<string> TriggerAssetIds { get; init; }
    /// <summary>First seed (legacy JSON / Kafka compat).</summary>
    public string TriggerAssetId => TriggerAssetIds.Count > 0 ? TriggerAssetIds[0] : "";
    /// <summary>상태 패치 표현 (currentTemp, currentPower, status, lastEventType 등).</summary>
    public IReadOnlyDictionary<string, object> Trigger { get; init; } = new Dictionary<string, object>();
    public int MaxDepth { get; init; }
    /// <summary>백그라운드 엔진 폴링 주기(ms). 1–5000.</summary>
    public int EngineTickIntervalMs { get; init; } = SimulationEngineConstants.DefaultEngineTickIntervalMs;
    /// <summary>Run 전역 tick 번호. 지속 Run에서만 엔진이 증가.</summary>
    public int TickIndex { get; init; }
    /// <summary>Run 시작 시점 참여 에셋 스냅샷 (assetId → { properties, status }). Phase 21 재현용.</summary>
    public IReadOnlyDictionary<string, object> InitialSnapshot { get; init; } = new Dictionary<string, object>();

    /// <summary>사용자 변조 이력.</summary>
    public IReadOnlyList<SimulationOverrideEntryDto> Overrides { get; init; } = Array.Empty<SimulationOverrideEntryDto>();
}
