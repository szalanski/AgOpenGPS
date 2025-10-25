using Microsoft.AspNetCore.SignalR;

namespace AgOpenGPS.Api.Hubs;

/// <summary>
/// SignalR Hub for broadcasting application state to connected clients.
/// Minimal implementation - used internally by SignalRStatePublisher.
/// </summary>
public class StateHub : Hub
{
    private readonly ILogger<StateHub> _logger;

    public StateHub(ILogger<StateHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
