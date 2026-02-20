namespace DotnetEngine.Application.Simulation.Dto;

/// <summary>
/// 시뮬레이션 런 실행 결과 DTO.
/// </summary>
public sealed record RunResult
{
    public required bool Success { get; init; }
    public required string RunId { get; init; }
    public required string Message { get; init; }
}
