using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Configuration options for connecting to the backend state service.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// Base URL of the backend service (e.g., "http://localhost:5000").
        /// </summary>
        public string Url { get; set; } = "http://localhost:5000";

        /// <summary>
        /// Enable automatic reconnection if the connection is lost.
        /// </summary>
        public bool EnableAutomaticReconnect { get; set; } = true;
    }
}
