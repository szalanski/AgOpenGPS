using AgOpenGPS.Api.Client.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace AgOpenGPS.Api.Hubs;

/// <summary>
/// SignalR Hub for broadcasting application state to connected clients.
/// Also handles commands from clients (bidirectional communication).
/// </summary>
public class StateHub : Hub
{
    private readonly ILogger<StateHub> _logger;
    private readonly IMediator _mediator;

    public StateHub(ILogger<StateHub> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
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

    #region Command Methods (Client → Server)

    /// <summary>
    /// Unified simulator command handler.
    /// Accepts all simulator control events via single endpoint.
    /// </summary>
    public async Task UpdateSimulator(UpdateSimulatorCommand command)
    {
        _logger.LogDebug("UpdateSimulator command from {ConnectionId}: Type={EventType}",
            Context.ConnectionId, command.Event.Type);
        await _mediator.Send(command);
    }

    /// <summary>
    /// Updates the local plane coordinate system origin.
    /// </summary>
    public async Task UpdateLocalPlane(UpdateLocalPlaneCommand command)
    {
        _logger.LogDebug("UpdateLocalPlane command from {ConnectionId}: Lat={Lat:F6}, Lon={Lon:F6}",
            Context.ConnectionId, command.Origin.Latitude, command.Origin.Longitude);
        await _mediator.Send(command);
    }

    #endregion
}
