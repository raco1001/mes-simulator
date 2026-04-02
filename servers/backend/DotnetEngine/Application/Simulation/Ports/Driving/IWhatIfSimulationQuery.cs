using DotnetEngine.Application.Simulation.Dto;

namespace DotnetEngine.Application.Simulation.Ports.Driving;

public interface IWhatIfSimulationQuery
{
    Task<WhatIfResult> RunAsync(RunSimulationRequest request, CancellationToken cancellationToken = default);
}
