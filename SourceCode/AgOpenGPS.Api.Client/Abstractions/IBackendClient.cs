using System;
using System.Threading.Tasks;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Client.Abstractions
{
    /// <summary>
    /// Transport-agnostic interface for bidirectional communication with backend.
    /// Receives state updates (backend → client) and sends commands (client → backend).
    /// Enables future implementations using WebSockets, gRPC, or other protocols.
    /// Implements IDisposable and IAsyncDisposable for proper resource cleanup.
    /// </summary>
    public interface IBackendClient : IDisposable, IAsyncDisposable
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
        /// Subscribe to receive application state updates from the backend (Query - backend → client).
        /// The client manages the subscription lifecycle internally.
        /// </summary>
        /// <param name="onNext">Callback invoked when a state update is received</param>
        /// <param name="onError">Optional callback invoked when an error occurs</param>
        void SubscribeToState(Action<ApplicationState> onNext, Action<Exception>? onError = null);

        /// <summary>
        /// Send a command to the backend (Command - client → backend).
        /// Commands are dispatched to appropriate handlers via MediatR on the backend.
        /// </summary>
        /// <typeparam name="TCommand">Command type implementing ICommand</typeparam>
        /// <param name="command">Command instance to send</param>
        Task SendCommandAsync<TCommand>(TCommand command) where TCommand : ICommand;

        /// <summary>
        /// Gets whether the client is currently connected to the backend.
        /// </summary>
        bool IsConnected { get; }
    }
}
