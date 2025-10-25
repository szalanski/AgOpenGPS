using System;
using Microsoft.AspNetCore.SignalR.Client;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;

namespace AgOpenGPS.Api.Client.Factories
{
    /// <summary>
    /// Factory for creating IStateSubscriber instances.
    /// Encapsulates the creation of HubConnection and SignalRStateSubscriber.
    /// </summary>
    public static class SubscriberFactory
    {
        /// <summary>
        /// Creates a SignalR-based state subscriber configured with the specified options.
        /// </summary>
        /// <param name="options">Connection configuration</param>
        /// <returns>Configured IStateSubscriber ready to connect</returns>
        public static IStateSubscriber CreateSignalRSubscriber(ConnectionOptions options)
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

            // Create and return SignalRStateSubscriber with injected HubConnection
            return new SignalRStateSubscriber(hubConnection);
        }
    }
}
