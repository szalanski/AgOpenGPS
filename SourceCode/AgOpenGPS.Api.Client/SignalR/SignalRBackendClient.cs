using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Client.SignalR
{
    /// <summary>
    /// SignalR implementation of IBackendClient.
    /// Provides bidirectional communication:
    /// - Receives application state updates from backend (backend → client)
    /// - Sends commands to backend (client → backend)
    /// </summary>
    public class SignalRBackendClient : IBackendClient
    {
        private readonly HubConnection _hubConnection;
        private readonly Subject<ApplicationState> _stateSubject = new Subject<ApplicationState>();
        private IDisposable? _subscription;

        /// <summary>
        /// Initializes a new instance of SignalRBackendClient.
        /// </summary>
        /// <param name="hubConnection">Pre-configured HubConnection to use for communication</param>
        public SignalRBackendClient(HubConnection hubConnection)
        {
            _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));

            // Register handler for state update messages (backend → client)
            _hubConnection.On<ApplicationState>("ReceiveState", state =>
            {
                _stateSubject.OnNext(state);
            });
        }

        /// <inheritdoc />
        public void SubscribeToState(Action<ApplicationState> onNext, Action<Exception>? onError = null)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            // Store subscription internally for disposal
            _subscription = onError != null
                ? _stateSubject.Subscribe(onNext, onError)
                : _stateSubject.Subscribe(onNext);
        }

        /// <inheritdoc />
        public async Task SendCommandAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!IsConnected)
                throw new InvalidOperationException("Not connected to backend. Call ConnectAsync() first.");

            // Route command to specific hub method based on type
            // Note: SignalR doesn't support generic hub methods, so we need specific methods for each command type
            switch (command)
            {
                case UpdateSimulatorCommand cmd:
                    await _hubConnection.InvokeAsync("UpdateSimulator", cmd);
                    break;

                case UpdateLocalPlaneCommand cmd:
                    await _hubConnection.InvokeAsync("UpdateLocalPlane", cmd);
                    break;

                default:
                    throw new NotSupportedException($"Command type '{command.GetType().Name}' is not supported. " +
                        "Add a new case to SignalRBackendClient.SendCommandAsync and a corresponding hub method.");
            }
        }

        /// <inheritdoc />
        public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

        /// <inheritdoc />
        public async Task ConnectAsync()
        {
            // Connect to the hub
            await _hubConnection.StartAsync();
        }

        /// <inheritdoc />
        public async Task DisconnectAsync()
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
                _stateSubject.OnCompleted(); // Signal stream end
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _subscription?.Dispose();
            DisconnectAsync().GetAwaiter().GetResult();
            _stateSubject.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _subscription?.Dispose();
            await DisconnectAsync();
            _stateSubject.Dispose();
        }
    }
}
