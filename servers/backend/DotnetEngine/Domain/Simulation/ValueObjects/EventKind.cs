namespace DotnetEngine.Domain.Simulation.ValueObjects;

/// <summary>
/// 이벤트 계열: Command(입력) vs Observation(출력).
/// Live 전환 시 명령부만 교체할 수 있도록 구분한다.
/// </summary>
public enum EventKind
{
    /// <summary>시스템에 대한 명령(입력). 사용자·외부가 발생.</summary>
    Command,

    /// <summary>관측 결과(출력). 엔진/시뮬레이터가 생성.</summary>
    Observation,
}
