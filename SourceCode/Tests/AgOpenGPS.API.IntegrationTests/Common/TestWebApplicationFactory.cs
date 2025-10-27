using AgOpenGPS.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        builder.ConfigureServices((context, services) =>
        {
            // Override UdpOptions configuration for tests
            // This replaces the configuration from appsettings.json in Program.cs
            services.PostConfigure<UdpOptions>(options =>
            {
                options.ListenPort = 15556; // Backend test port (avoid conflict with FormGPS on 15555)
                options.BufferSize = 1024;
            });

            // No other overrides needed - use real UdpPacketReceiver with real UDP sockets!
            // HTTP/SignalR will use TestServer (in-memory)
            // UDP will use real UdpClient (real OS socket on port 15556)
        });
    }
}
