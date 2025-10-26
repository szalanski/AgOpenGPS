using AgOpenGPS.Api.Models;

namespace AgOpenGPS.Api.Abstractions;

/// <summary>
/// Interface for UDP packet receivers.
/// Allows dependency injection and testing with different implementations.
/// </summary>
public interface IUdpPacketReceiver : IDisposable
{
    /// <summary>
    /// Asynchronously stream validated UDP packets.
    /// </summary>
    IAsyncEnumerable<UdpPacket> GetPacketsAsync(CancellationToken cancellationToken = default);
}
