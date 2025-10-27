using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Models;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// Main application orchestrator - GPS-driven (not timer-based).
/// Receives UDP packets from AgIO, processes GPS data, broadcasts state via SignalR.
/// </summary>
public class ApplicationOrchestrator : BackgroundService
{
    private readonly ILogger<ApplicationOrchestrator> _logger;
    private readonly IStatePublisher _statePublisher;
    private readonly IGnssService _gnssService;
    private readonly IUdpPacketReceiver _udpReceiver;

    public ApplicationOrchestrator(
        ILogger<ApplicationOrchestrator> logger,
        IStatePublisher statePublisher,
        IGnssService gnssService,
        IUdpPacketReceiver udpReceiver)
    {
        _logger = logger;
        _statePublisher = statePublisher;
        _gnssService = gnssService;
        _udpReceiver = udpReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ApplicationOrchestrator starting - GPS-driven UDP mode");

        // TODO: Initialize local plane (will be configurable in future)
        // For now, using placeholder coordinates for testing
        _gnssService.InitializeLocalPlane(new Wgs84Position(45.0, -93.0));

        // Main loop: Consume UDP packets and broadcast GPS state
        await foreach (var packet in _udpReceiver.GetPacketsAsync(stoppingToken))
        {
            try
            {
                await ProcessPacketAsync(packet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {PacketType} packet", packet.Type);
            }
        }

        _logger.LogInformation("ApplicationOrchestrator stopped");
    }

    /// <summary>
    /// Process incoming UDP packet by type and broadcast GPS state.
    /// </summary>
    private async Task ProcessPacketAsync(UdpPacket packet)
    {
        switch (packet.Type)
        {
            case UdpPacketType.Gps:
                // Process GPS packet
                _gnssService.ProcessGpsPacket(packet.Data);

                // Get current GPS state
                var gnssState = _gnssService.GetCurrentState();

                // Broadcast via SignalR (only if GPS data is valid)
                if (gnssState != null)
                {
                    var appState = new ApplicationState
                    {
                        Timestamp = DateTime.UtcNow,
                        Gnss = gnssState
                    };

                    await _statePublisher.BroadcastStateAsync(appState);
                }
                break;

            case UdpPacketType.Imu:
                _logger.LogDebug("IMU packet received - not yet implemented");
                break;

            case UdpPacketType.Disconnect:
                _logger.LogInformation("Disconnect packet received");
                break;

            case UdpPacketType.AutoSteer:
                _logger.LogDebug("AutoSteer packet received - not yet implemented");
                break;

            default:
                _logger.LogDebug("Received {PacketType} packet - not yet implemented", packet.Type);
                break;
        }
    }
}
