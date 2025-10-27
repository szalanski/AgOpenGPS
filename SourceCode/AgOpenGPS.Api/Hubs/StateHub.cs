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
    /// Start the GPS simulator with specified parameters.
    /// </summary>
    public async Task StartSimulator(StartSimulatorCommand command)
    {
        _logger.LogInformation("StartSimulator command from {ConnectionId}: Lat={Lat}, Lon={Lon}, Heading={Heading}°, Speed={Speed} km/h",
            Context.ConnectionId, command.Latitude, command.Longitude, command.HeadingDegrees, command.SpeedKmh);
        await _mediator.Send(command);
    }

    /// <summary>
    /// Stop the GPS simulator.
    /// </summary>
    public async Task StopSimulator(StopSimulatorCommand command)
    {
        _logger.LogInformation("StopSimulator command from {ConnectionId}", Context.ConnectionId);
        await _mediator.Send(command);
    }

    /// <summary>
    /// Set the simulator speed.
    /// </summary>
    public async Task SetSimulatorSpeed(SetSimulatorSpeedCommand command)
    {
        _logger.LogInformation("SetSimulatorSpeed command from {ConnectionId}: Speed={Speed} km/h",
            Context.ConnectionId, command.SpeedKmh);
        await _mediator.Send(command);
    }

    /// <summary>
    /// Set the simulator steering angle.
    /// </summary>
    public async Task SetSimulatorSteering(SetSimulatorSteeringCommand command)
    {
        _logger.LogInformation("SetSimulatorSteering command from {ConnectionId}: Angle={Angle}°",
            Context.ConnectionId, command.SteerAngle);
        await _mediator.Send(command);
    }

    /// <summary>
    /// Reset the simulator to default state.
    /// </summary>
    public async Task ResetSimulator(ResetSimulatorCommand command)
    {
        _logger.LogInformation("ResetSimulator command from {ConnectionId}", Context.ConnectionId);
        await _mediator.Send(command);
    }

    #endregion
}
