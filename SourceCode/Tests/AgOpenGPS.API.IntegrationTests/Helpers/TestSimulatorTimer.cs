using AgOpenGPS.Api.Abstractions;

namespace AgOpenGPS.API.IntegrationTests.Helpers;

/// <summary>
/// Test implementation of ISimulatorTimer using manual tick advancement.
/// Allows tests to synchronously control when simulator ticks occur without delays.
/// </summary>
public class TestSimulatorTimer : ISimulatorTimer
{
    private readonly SemaphoreSlim _tickSignal = new(0);

    /// <summary>
    /// Wait for next simulator tick (released when AdvanceTick called).
    /// </summary>
    public async Task WaitForNextTickAsync(CancellationToken cancellationToken)
    {
        await _tickSignal.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Manually advance simulator tick synchronously.
    /// Called by tests to trigger next packet generation without delays.
    /// </summary>
    public void AdvanceTick()
    {
        _tickSignal.Release();
    }
}
