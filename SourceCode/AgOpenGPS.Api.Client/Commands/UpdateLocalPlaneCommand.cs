using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Client.Commands
{
    /// <summary>
    /// Command to update local plane coordinate system origin.
    /// Single responsibility: coordinate transformation updates only.
    /// </summary>
    public record UpdateLocalPlaneCommand(Wgs84Position Origin) : ICommand;
}
