using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// Main application orchestrator that runs at 10 Hz (every 100ms).
/// Generates application state and broadcasts to all connected clients.
/// </summary>
public class ApplicationOrchestrator : IHostedService, IDisposable
{
    private readonly ILogger<ApplicationOrchestrator> _logger;
    private readonly IStatePublisher _statePublisher;
    private Timer? _timer;
    private bool _isRunning;

    public ApplicationOrchestrator(
        ILogger<ApplicationOrchestrator> logger,
        IStatePublisher statePublisher)
    {
        _logger = logger;
        _statePublisher = statePublisher;
    }

    /// <summary>
    /// Start the orchestrator when the application starts.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApplicationOrchestrator starting - 10 Hz tick loop");

        // Create timer with 100ms interval (10 Hz)
        _timer = new Timer(
            callback: OnTick,
            state: null,
            dueTime: TimeSpan.Zero,      // Start immediately
            period: TimeSpan.FromMilliseconds(100)); // 10 Hz

        _isRunning = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the orchestrator when the application stops.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApplicationOrchestrator stopping");

        _isRunning = false;
        _timer?.Change(Timeout.Infinite, 0); // Stop timer

        return Task.CompletedTask;
    }

    /// <summary>
    /// Executed every 100ms (10 Hz).
    /// Generates application state and broadcasts to clients.
    /// </summary>
    private async void OnTick(object? state)
    {
        if (!_isRunning)
            return;

        try
        {
            // Generate application state with current timestamp
            var appState = new ApplicationState
            {
                Timestamp = DateTime.UtcNow
            };

            // Broadcast to all connected clients
            await _statePublisher.BroadcastStateAsync(appState);

            _logger.LogInformation("Tick at {Timestamp}", appState.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tick execution");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
