using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Abstractions;

/// <summary>
/// Abstraction for broadcasting application state to clients.
/// Decouples ApplicationOrchestrator from specific transport (SignalR, WebSockets, etc.).
/// </summary>
public interface IStatePublisher
{
    /// <summary>
    /// Broadcast application state to all connected clients.
    /// </summary>
    /// <param name="state">The application state to broadcast</param>
    Task BroadcastStateAsync(ApplicationState state);
}
