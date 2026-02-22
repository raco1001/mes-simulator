using System.Collections.Generic;
using DotnetEngine.Domain.Simulation.ValueObjects;

namespace DotnetEngine.Domain.Simulation.Constants;

/// <summary>
/// 이벤트 타입 상수 및 Command/Observation 분류 헬퍼.
/// 시뮬레이터는 Observation만 생성; Live 시 Adapter가 Observation 주입.
/// </summary>
public static class EventTypes
{
    // --- Observation (시뮬레이터·Live Adapter가 생성/주입) ---

    /// <summary>시뮬레이션 전파로 에셋 상태 갱신. 현재 주로 사용.</summary>
    public const string SimulationStateUpdated = "simulation.state.updated";

    /// <summary>전력 값 변경.</summary>
    public const string PowerChanged = "power_changed";

    /// <summary>상태 전이 (예: normal → warning).</summary>
    public const string StateTransitioned = "state_transitioned";

    // --- Command (시뮬레이터는 생성하지 않음, 타입·문서만 정의) ---

    /// <summary>기기/머신 시작 명령.</summary>
    public const string StartMachine = "start_machine";

    /// <summary>기기/머신 정지 명령.</summary>
    public const string StopMachine = "stop_machine";

    /// <summary>속도 변경 명령.</summary>
    public const string ChangeSpeed = "change_speed";

    private static readonly HashSet<string> ObservationSet = new(StringComparer.Ordinal)
    {
        SimulationStateUpdated,
        PowerChanged,
        StateTransitioned,
    };

    private static readonly HashSet<string> CommandSet = new(StringComparer.Ordinal)
    {
        StartMachine,
        StopMachine,
        ChangeSpeed,
    };

    /// <summary>
    /// eventType 문자열로 EventKind를 반환한다.
    /// 알 수 없는 타입은 Observation으로 간주한다 (기존 호환).
    /// </summary>
    public static EventKind GetKind(string? eventType)
    {
        if (string.IsNullOrEmpty(eventType))
            return EventKind.Observation;
        if (CommandSet.Contains(eventType))
            return EventKind.Command;
        return EventKind.Observation;
    }
}
