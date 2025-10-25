using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgOpenGPS.API.IntegrationTests.Common;

/// <summary>
/// Custom WebApplicationFactory for hosting the API in-memory for integration tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Can override services here if needed for testing
        });
    }
}
