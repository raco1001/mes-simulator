namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 상태 패치 (시뮬레이션 트리거/전파 시 적용할 필드만 지정).
/// </summary>
public sealed record StatePatchDto
{
    public double? CurrentTemp { get; init; }
    public double? CurrentPower { get; init; }
    public string? Status { get; init; }
    public string? LastEventType { get; init; }
}
