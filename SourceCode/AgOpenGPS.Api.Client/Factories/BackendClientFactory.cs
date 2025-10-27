using System;
using Microsoft.AspNetCore.SignalR.Client;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;

namespace AgOpenGPS.Api.Client.Factories
{
    /// <summary>
    /// Factory for creating IBackendClient instances.
    /// Encapsulates the creation of HubConnection and SignalRBackendClient.
    /// </summary>
    public static class BackendClientFactory
    {
        /// <summary>
        /// Creates a SignalR-based backend client configured with the specified options.
        /// Provides bidirectional communication (state updates + commands).
        /// </summary>
        /// <param name="options">Connection configuration</param>
        /// <returns>Configured IBackendClient ready to connect</returns>
        public static IBackendClient CreateSignalRClient(ConnectionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.Url))
                throw new ArgumentException("URL cannot be null or empty", nameof(options));

            // Build HubConnection with configuration
            var builder = new HubConnectionBuilder()
                .WithUrl($"{options.Url}/statehub");

            if (options.EnableAutomaticReconnect)
            {
                builder = builder.WithAutomaticReconnect();
            }

            var hubConnection = builder.Build();

            // Create and return SignalRBackendClient with injected HubConnection
            return new SignalRBackendClient(hubConnection);
        }
    }
}
