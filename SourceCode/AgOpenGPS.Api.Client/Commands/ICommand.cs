using MediatR;

namespace AgOpenGPS.Api.Client.Commands
{
    /// <summary>
    /// Marker interface for commands sent from client to backend.
    /// All commands must implement IRequest from MediatR for command dispatch.
    /// </summary>
    public interface ICommand : IRequest
    {
    }
}
