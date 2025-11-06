using System.Collections.Concurrent;
using AgOpenGPS.Api.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace AgOpenGPS.API.IntegrationTests.Common;

/// <summary>
/// Base class for all integration tests.
/// Each fixture gets isolated factory instance with unique UDP port.
/// Tests run sequentially (not in parallel) to avoid SignalR HubConnection state conflicts.
/// </summary>
[TestFixture]
public abstract class BaseIntegrationTest
{
    protected TestWebApplicationFactory? _factory;
    protected HttpClient? _httpClient;

    /// <summary>
    /// UDP port assigned to this test fixture (hardcoded per fixture class).
    /// Each derived class must override this property.
    /// Example: 15556 for GpsPacketProcessingTests, 15557 for SimulatorIntegrationTests, etc.
    /// </summary>
    protected abstract int TestFixturePort { get; }

    /// <summary>
    /// Create isolated factory instance for this fixture with hardcoded port.
    /// </summary>
    [OneTimeSetUp]
    public virtual void InitializeFixture()
    {
        _factory = new TestWebApplicationFactory(TestFixturePort);
        _httpClient = _factory.CreateClient();
    }

    /// <summary>
    /// Dispose factory and HttpClient after all tests complete.
    /// </summary>
    [OneTimeTearDown]
    public virtual void CleanupFixture()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Get the factory instance for this fixture.
    /// </summary>
    protected TestWebApplicationFactory Factory => _factory ?? throw new InvalidOperationException("Factory not initialized");

    /// <summary>
    /// Get the HttpClient instance for this fixture.
    /// </summary>
    protected HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("HttpClient not initialized");

    /// <summary>
    /// UDP port allocated to this fixture instance (unique per fixture).
    /// </summary>
    protected int UdpPort => Factory.UdpPort;

    /// <summary>
    /// Backend URL derived from HttpClient.
    /// </summary>
    protected string BackendUrl => HttpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;

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

    /// <summary>
    /// Creates a thread-safe collection for capturing ApplicationState in tests.
    /// Centralized factory allows changing collection type in one place.
    /// Uses ConcurrentQueue to preserve insertion order (FIFO) for timestamp validation.
    /// </summary>
    protected static ConcurrentQueue<ApplicationState> CreateStateCollection()
        => new ConcurrentQueue<ApplicationState>();
}
