using AgOpenGPS.Api.Abstractions;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// Production implementation of ISimulatorTimer using .NET PeriodicTimer (93ms intervals).
/// </summary>
public class ProductionSimulatorTimer : ISimulatorTimer
{
    private readonly PeriodicTimer _timer;

    /// <summary>
    /// Initialize production timer with 93ms interval.
    /// </summary>
    public ProductionSimulatorTimer()
    {
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(93));
    }

    /// <summary>
    /// Wait for next timer tick (93ms).
    /// </summary>
    public async Task WaitForNextTickAsync(CancellationToken cancellationToken)
    {
        await _timer.WaitForNextTickAsync(cancellationToken);
    }
}
