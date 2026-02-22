using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Rules;

/// <summary>
/// 관계 타입별 상태 전파 규칙. BFS 시 첫 번째로 CanApply가 true인 룰만 적용.
/// </summary>
public interface IPropagationRule
{
    bool CanApply(PropagationContext ctx);
    PropagationResult Apply(PropagationContext ctx);
}
