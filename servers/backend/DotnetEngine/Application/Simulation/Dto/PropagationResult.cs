namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 전파 룰 적용 결과: 대상 에셋에 적용할 패치와 생성할 도메인 이벤트.
/// </summary>
public sealed record PropagationResult
{
    public required StatePatchDto OutgoingPatch { get; init; }
    public IReadOnlyList<EventDto> Events { get; init; } = [];
}
