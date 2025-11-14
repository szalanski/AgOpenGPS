using System;

namespace AgOpenGPS.Api.Client.SignalR
{
    /// <summary>
    /// Resolves SignalR hub method names for client command types.
    /// </summary>
    public interface ISignalRCommandRouter
    {
        /// <summary>
        /// Attempts to resolve the hub method name for the specified command type.
        /// </summary>
        /// <param name="commandType">Command type implementing <c>ICommand</c>.</param>
        /// <param name="hubMethodName">Resolved hub method name when available.</param>
        /// <returns><c>true</c> when the command type has a registered hub method; otherwise <c>false</c>.</returns>
        bool TryGetHubMethodForCommand(Type commandType, out string hubMethodName);

        /// <summary>
        /// Resolves the hub method name for the specified command type.
        /// </summary>
        /// <param name="commandType">Command type implementing <c>ICommand</c>.</param>
        /// <returns>The hub method name associated with the command.</returns>
        /// <exception cref="NotSupportedException">Thrown when the command type does not have a mapping.</exception>
        string GetHubMethodForCommand(Type commandType);
    }
}
