using System;
using System.Threading.Tasks;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Client.Abstractions
{
    /// <summary>
    /// Transport-agnostic interface for subscribing to application state updates.
    /// Enables future implementations using WebSockets, gRPC, or other protocols.
    /// </summary>
    public interface IStateSubscriber
    {
        /// <summary>
        /// Connect to the backend.
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Disconnect from the backend.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Subscribe to receive application state updates from the backend.
        /// </summary>
        /// <param name="onNext">Callback invoked when a state update is received</param>
        /// <param name="onError">Optional callback invoked when an error occurs</param>
        /// <returns>Disposable subscription that can be disposed to unsubscribe</returns>
        IDisposable Subscribe(Action<ApplicationState> onNext, Action<Exception>? onError = null);

        /// <summary>
        /// Gets whether the client is currently connected to the backend.
        /// </summary>
        bool IsConnected { get; }
    }
}
