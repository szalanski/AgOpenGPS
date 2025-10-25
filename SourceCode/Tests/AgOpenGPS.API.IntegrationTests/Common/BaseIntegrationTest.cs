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
    public void OneTimeSetUp()
    {
        Factory = new TestWebApplicationFactory();
        HttpClient = Factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        HttpClient?.Dispose();
        Factory?.Dispose();
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
