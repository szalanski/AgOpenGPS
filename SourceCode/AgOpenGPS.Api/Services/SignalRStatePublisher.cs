using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// SignalR implementation of IStatePublisher.
/// Broadcasts application state to all connected clients via SignalR.
/// </summary>
public class SignalRStatePublisher : IStatePublisher
{
    private readonly IHubContext<StateHub> _hubContext;
    private readonly ILogger<SignalRStatePublisher> _logger;

    public SignalRStatePublisher(
        IHubContext<StateHub> hubContext,
        ILogger<SignalRStatePublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcast application state to all connected SignalR clients.
    /// </summary>
    public async Task BroadcastStateAsync(ApplicationState state)
    {
        try
        {
            // Broadcast to all connected clients using the "ReceiveState" method
            await _hubContext.Clients.All.SendAsync("ReceiveState", state);

            // Use Debug level to avoid log spam (10 Hz = lots of logs)
            _logger.LogDebug("Broadcasted state at {Timestamp} to all clients", state.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting state at {Timestamp}", state.Timestamp);
        }
    }
}
