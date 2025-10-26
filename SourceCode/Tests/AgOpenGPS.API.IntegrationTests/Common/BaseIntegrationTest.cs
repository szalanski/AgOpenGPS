using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR.Client;

namespace AgOpenGPS.API.IntegrationTests.Common;

/// <summary>
/// Base class for all integration tests.
/// Manages the test web application factory and HTTP client lifecycle.
/// </summary>
[TestFixture]
public abstract class BaseIntegrationTest
{
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient HttpClient { get; private set; } = null!;
    protected string BackendUrl => HttpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Check if port 15556 is available (backend uses 15556, FormGPS uses 15555)
        if (IsPortInUse(15556))
        {
            Assert.Fail("Port 15556 is already in use. Backend must bind to this port for tests.");
        }

        Factory = new TestWebApplicationFactory();
        HttpClient = Factory.CreateClient();

        // Wait for ApplicationOrchestrator BackgroundService to start and bind UDP socket
        // BackgroundServices start asynchronously, need extra time to fully initialize
        await Task.Delay(2000);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        HttpClient?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    /// Check if a UDP port is already in use.
    /// </summary>
    private bool IsPortInUse(int port)
    {
        try
        {
            using var udpClient = new UdpClient(port);
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }

    /// <summary>
    /// Creates a HubConnection configured for testing with the in-memory test server.
    /// </summary>
    /// <param name="hubPath">Hub path (e.g., "/statehub")</param>
    /// <returns>Configured HubConnection ready for testing</returns>
    protected HubConnection CreateTestHubConnection(string hubPath)
    {
        return new HubConnectionBuilder()
            .WithUrl($"{BackendUrl}{hubPath}", options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();
    }
}
