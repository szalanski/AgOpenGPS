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
    private readonly ICoordinateService _coordinateService;
    private readonly SimulatorService _simulatorService;
    private readonly IUdpPacketReceiver _udpReceiver;

    public ApplicationOrchestrator(
        ILogger<ApplicationOrchestrator> logger,
        IStatePublisher statePublisher,
        IGnssService gnssService,
        ICoordinateService coordinateService,
        SimulatorService simulatorService,
        IUdpPacketReceiver udpReceiver)
    {
        _logger = logger;
        _statePublisher = statePublisher;
        _gnssService = gnssService;
        _coordinateService = coordinateService;
        _simulatorService = simulatorService;
        _udpReceiver = udpReceiver;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ApplicationOrchestrator starting - GPS-driven UDP mode");

        // Local plane initialization happens in two scenarios:
        // 1. Simulator mode: SimulatorService.ProcessEvent(Start) initializes on simulator start
        // 2. Real GPS mode: GnssService.ProcessGpsPacket() initializes on first valid GPS fix
        // This ensures coordinate transformations use actual GPS position, not placeholder

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
                        Gnss = gnssState,
                        LocalPlane = _coordinateService.GetLocalPlaneInfo(),
                        Control = new ControlState
                        {
                            ActualSteeringAngle = _simulatorService.GetCurrentSteering()
                        }
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
