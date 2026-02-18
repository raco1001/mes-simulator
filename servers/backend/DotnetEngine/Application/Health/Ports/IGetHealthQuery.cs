using DotnetEngine.Application.Health.Dto;

namespace DotnetEngine.Application.Health.Ports;

public interface IGetHealthQuery
{
    Task<HealthStatusDto> GetAsync(CancellationToken cancellationToken = default);
}
