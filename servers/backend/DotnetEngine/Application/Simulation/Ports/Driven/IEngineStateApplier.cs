using DotnetEngine.Application.Asset.Dto;
using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// 엔진 상태 변경 진입점 (Driven Port). 이벤트 적용 시 상태 저장 + 이벤트 저장/발행.
/// Live 교체 시 동일 엔진을 쓰기 위해 단일 경로로 수렴.
/// </summary>
public interface IEngineStateApplier
{
    /// <summary>
    /// 병합된 상태를 저장하고, 이벤트를 append·발행한다. (UpsertState → Append → Publish 순서.)
    /// </summary>
    Task ApplyAsync(EventDto evt, StateDto mergedState, CancellationToken cancellationToken = default);
}
