using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Abstractions;

/// <summary>
/// Service contract for GPS/GNSS data processing.
/// Receives binary UDP packets from AgIO, unpacks data, performs coordinate transformations,
/// and maintains current GPS state for broadcasting.
/// </summary>
public interface IGnssService
{
    /// <summary>
    /// Process incoming binary GPS packet from AgIO (PGN 0xD6).
    /// Unpacks binary protocol, transforms coordinates, calculates GPS frequency,
    /// and updates internal GNSS state.
    /// </summary>
    /// <param name="packetData">Binary UDP packet data from AgIO</param>
    void ProcessGpsPacket(byte[] packetData);

    /// <summary>
    /// Get current GNSS state for broadcasting to clients.
    /// </summary>
    /// <returns>Current GPS state, or null if no valid GPS data received yet</returns>
    GnssState? GetCurrentState();
}
