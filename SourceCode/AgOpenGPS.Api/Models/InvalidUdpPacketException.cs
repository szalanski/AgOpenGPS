namespace AgOpenGPS.Api.Models;

/// <summary>
/// Exception thrown when UDP packet validation fails.
/// Contains details about which AgIO protocol validation rule was violated.
/// </summary>
public class InvalidUdpPacketException : Exception
{
    public InvalidUdpPacketException(string message) : base(message)
    {
    }

    public InvalidUdpPacketException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
