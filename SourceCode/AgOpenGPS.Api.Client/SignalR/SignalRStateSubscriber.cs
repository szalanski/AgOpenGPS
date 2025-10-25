using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Client.SignalR
{
    /// <summary>
    /// SignalR implementation of IStateSubscriber.
    /// Connects to the backend StateHub and receives application state updates.
    /// </summary>
    public class SignalRStateSubscriber : IStateSubscriber
    {
        private readonly HubConnection _hubConnection;
        private readonly Subject<ApplicationState> _stateSubject = new Subject<ApplicationState>();

        /// <summary>
        /// Initializes a new instance of SignalRStateSubscriber.
        /// </summary>
        /// <param name="hubConnection">Pre-configured HubConnection to use for communication</param>
        public SignalRStateSubscriber(HubConnection hubConnection)
        {
            _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));

            // Register handler for state update messages
            _hubConnection.On<ApplicationState>("ReceiveState", state =>
            {
                _stateSubject.OnNext(state);
            });
        }

        /// <inheritdoc />
        public IDisposable Subscribe(Action<ApplicationState> onNext, Action<Exception>? onError = null)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));

            if (onError != null)
                return _stateSubject.Subscribe(onNext, onError);
            else
                return _stateSubject.Subscribe(onNext);
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
    }
}
