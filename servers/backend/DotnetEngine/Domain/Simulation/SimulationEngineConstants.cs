namespace DotnetEngine.Domain.Simulation;

/// <summary>
/// Continuous simulation engine timing, propagation defaults, and future cycle-policy hooks.
/// </summary>
public static class SimulationEngineConstants
{
    public const int MinEngineTickIntervalMs = 1;
    public const int MaxEngineTickIntervalMs = 5000;
    public const int DefaultEngineTickIntervalMs = 1000;

    /// <summary>
    /// When MaxDepth is omitted or non-positive, use this BFS depth so propagation reaches typical graph leaves.
    /// </summary>
    public const int DefaultLeafPropagationMaxDepth = 100;

    /// <summary>
    /// Reserved for future event-loop / circular-graph resolution (iterative patch application cap).
    /// </summary>
    public const int MaxCycleResolutionIterations = 10;

    public static int ClampEngineTickIntervalMs(int value)
    {
        if (value < MinEngineTickIntervalMs)
            return MinEngineTickIntervalMs;
        if (value > MaxEngineTickIntervalMs)
            return MaxEngineTickIntervalMs;
        return value;
    }

    /// <summary>
    /// Wall-clock delta for Rate/Accumulator; clamped to the same bounds as engine tick for stability.
    /// </summary>
    public static TimeSpan ClampSimulationDelta(TimeSpan elapsed)
    {
        var ms = elapsed.TotalMilliseconds;
        if (double.IsNaN(ms) || ms < MinEngineTickIntervalMs)
            ms = MinEngineTickIntervalMs;
        else if (ms > MaxEngineTickIntervalMs)
            ms = MaxEngineTickIntervalMs;
        return TimeSpan.FromMilliseconds(ms);
    }
}
