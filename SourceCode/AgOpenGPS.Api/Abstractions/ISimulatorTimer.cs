namespace AgOpenGPS.Api.Abstractions;

/// <summary>
/// Abstraction for simulator timing mechanism.
/// Allows production code to use PeriodicTimer while tests use manual advancement.
/// </summary>
public interface ISimulatorTimer
{
    /// <summary>
    /// Wait for the next simulator tick (93ms interval in production).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when next tick occurs</returns>
    Task WaitForNextTickAsync(CancellationToken cancellationToken);
}
