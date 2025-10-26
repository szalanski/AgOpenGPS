namespace AgOpenGPS.Api.Configuration;

/// <summary>
/// Configuration options for UDP listener.
/// Bound from appsettings.json "Udp" section.
/// </summary>
public class UdpOptions
{
    public const string SectionName = "Udp";

    /// <summary>
    /// UDP port to listen on for AgIO packets.
    /// Default: 15555 (matches FormGPS loopback port)
    /// </summary>
    public int ListenPort { get; set; } = 15555;

    /// <summary>
    /// UDP receive buffer size in bytes.
    /// Default: 1024 bytes
    /// </summary>
    public int BufferSize { get; set; } = 1024;
}
