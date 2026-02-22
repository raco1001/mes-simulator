using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driven;

/// <summary>
/// 시뮬레이션 이벤트를 Kafka 등으로 발행하는 Port (Secondary/Driven).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(EventDto dto, CancellationToken cancellationToken = default);
}
