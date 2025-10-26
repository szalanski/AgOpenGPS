using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Configuration;
using AgOpenGPS.Api.Models;
using Microsoft.Extensions.Options;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// Receives and validates UDP packets from AgIO.
/// Implements AgIO binary protocol validation (header, checksum, length).
/// Registered as singleton in DI container.
/// </summary>
public class UdpPacketReceiver : IUdpPacketReceiver
{
    private readonly ILogger<UdpPacketReceiver> _logger;
    private readonly UdpClient _udpClient;
    private readonly UdpOptions _options;

    public UdpPacketReceiver(
        ILogger<UdpPacketReceiver> logger,
        IOptions<UdpOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Initialize UDP client on loopback interface
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, _options.ListenPort));

        _logger.LogInformation("UDP receiver initialized on port {Port}", _options.ListenPort);
    }

    /// <summary>
    /// Asynchronously stream validated UDP packets.
    /// Invalid packets are logged and skipped.
    /// </summary>
    public async IAsyncEnumerable<UdpPacket> GetPacketsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UDP packet receiver started");

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _udpClient.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UDP receiver cancelled");
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving UDP packet");
                continue;
            }

            // Validate packet using domain model
            UdpPacket? packet = null;
            try
            {
                packet = UdpPacket.Create(result.Buffer);
            }
            catch (InvalidUdpPacketException ex)
            {
                _logger.LogWarning(ex, "Invalid packet received");
            }

            if (packet != null)
            {
                yield return packet;
            }
        }
    }

    public void Dispose()
    {
        _udpClient?.Dispose();
        _logger.LogInformation("UDP receiver disposed");
    }
}
