using System.Net;
using System.Net.Sockets;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Configuration;
using AgOpenGPS.API.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgOpenGPS.API.IntegrationTests.Common;

/// <summary>
/// Custom WebApplicationFactory for hosting the API in-memory for integration tests.
/// Uses REAL UDP sockets while keeping HTTP/SignalR in-memory.
/// Each test fixture gets isolated factory instance with hardcoded unique UDP port.
/// Injects TestSimulatorTimer for synchronous test-driven simulator advancement.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// UDP port assigned to this factory instance (hardcoded per fixture).
    /// </summary>
    public int UdpPort { get; private set; }

    /// <summary>
    /// Test timer for synchronously advancing simulator ticks in tests.
    /// </summary>
    public TestSimulatorTimer TestTimer { get; }

    /// <summary>
    /// Constructor: Factory configured for specific UDP port.
    /// </summary>
    /// <param name="udpPort">Hardcoded port for this fixture (e.g., 15556, 15557, 15558)</param>
    public TestWebApplicationFactory(int udpPort)
    {
        UdpPort = udpPort;
        TestTimer = new TestSimulatorTimer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            // Override UdpOptions configuration for tests
            // Use isolated port per fixture instance
            services.PostConfigure<UdpOptions>(options =>
            {
                options.ListenPort = UdpPort; // Hardcoded port unique per fixture
                options.BufferSize = 1024;
            });

            // Override ISimulatorTimer with test timer for synchronous tick advancement
            services.AddSingleton<ISimulatorTimer>(TestTimer);

            // No other overrides needed - use real UdpPacketReceiver with real UDP sockets!
            // HTTP/SignalR will use TestServer (in-memory)
            // UDP will use real UdpClient (real OS socket on assigned port)
        });
    }
}
