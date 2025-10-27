using AgOpenGPS.Api.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// Background service that runs the GPS simulator at 93ms intervals.
    /// Sends generated GPS packets via UDP to localhost (same path as real GPS from AgIO).
    /// This ensures simulator uses identical pipeline: UDP → UdpPacketReceiver → ApplicationOrchestrator → GnssService.
    /// </summary>
    public class SimulatorHostedService : BackgroundService, IDisposable
    {
        private readonly SimulatorService _simulator;
        private readonly ILogger<SimulatorHostedService> _logger;
        private readonly UdpClient _udpClient;
        private readonly int _udpPort;

        public SimulatorHostedService(
            SimulatorService simulator,
            IOptions<UdpOptions> udpOptions,
            ILogger<SimulatorHostedService> logger)
        {
            _simulator = simulator;
            _logger = logger;
            _udpPort = udpOptions.Value.ListenPort;
            _udpClient = new UdpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SimulatorHostedService starting - will send UDP packets to localhost:{Port}", _udpPort);

            // Use PeriodicTimer (available in .NET 6+)
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(93)); // Match FormGPS timerSim

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);

                    // Get simulated GPS packet
                    var packet = _simulator.Tick();

                    if (packet != null)
                    {
                        // Send packet via UDP to localhost (same as real GPS from AgIO)
                        // This ensures simulator uses the full UDP → UdpPacketReceiver → GnssService pipeline
                        await _udpClient.SendAsync(packet, packet.Length, "127.0.0.1", _udpPort);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in simulator tick");
                }
            }

            _logger.LogInformation("SimulatorHostedService stopped");
        }

        public override void Dispose()
        {
            _udpClient?.Dispose();
            base.Dispose();
        }
    }
}
