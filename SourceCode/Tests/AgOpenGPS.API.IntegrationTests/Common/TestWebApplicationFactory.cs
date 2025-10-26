using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgOpenGPS.API.IntegrationTests.Common;

/// <summary>
/// Custom WebApplicationFactory for hosting the API in-memory for integration tests.
/// Uses REAL UDP sockets (port 15556) while keeping HTTP/SignalR in-memory.
/// Backend uses port 15556 to avoid conflict with FormGPS (which uses 15555).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // No overrides needed - use real UdpPacketReceiver with real UDP sockets!
            // HTTP/SignalR will use TestServer (in-memory)
            // UDP will use real UdpClient (real OS socket on port 15556)
        });
    }
}
