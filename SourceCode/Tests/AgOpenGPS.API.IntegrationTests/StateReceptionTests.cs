using System.Collections.Concurrent;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;
using AgOpenGPS.API.IntegrationTests.Common;
using FluentAssertions;

namespace AgOpenGPS.API.IntegrationTests;

/// <summary>
/// Integration tests for state reception via SignalR.
/// Validates that clients successfully receive ApplicationState updates from the backend.
/// Note: These tests start the backend simulator to generate state updates, since
/// ApplicationOrchestrator is event-driven and only broadcasts when UDP packets arrive.
/// </summary>
[TestFixture]
public class StateReceptionTests : BaseIntegrationTest
{
    private IBackendClient? _simulatorClient;

    [SetUp]
    public async Task SetUp()
    {
        // Start backend simulator to generate state updates
        // ApplicationOrchestrator is event-driven (no periodic broadcasts without GPS data)
        var hubConnection = CreateTestHubConnection("/statehub");
        _simulatorClient = new SignalRBackendClient(hubConnection);
        await _simulatorClient.ConnectAsync();

        await _simulatorClient.SendCommandAsync(
            new UpdateSimulatorCommand(SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0)))); // Generate GPS at ~93ms intervals

        await Task.Delay(500); // Let simulator start
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_simulatorClient != null)
        {
            try
            {
                await _simulatorClient.SendCommandAsync(new UpdateSimulatorCommand(SimulatorEvent.Stop()));
            }
            catch
            {
                // Ignore errors during cleanup
            }
            await _simulatorClient.DisposeAsync();
        }
    }

    [Test]
    public async Task SignalRStateSubscriber_ShouldConnect_ToBackend()
    {
        // Arrange
        var hubConnection = CreateTestHubConnection("/statehub");
        var subscriber = new SignalRStateSubscriber(hubConnection);

        // Act
        await subscriber.ConnectAsync();

        // Assert
        subscriber.IsConnected.Should().BeTrue();

        // Cleanup
        await subscriber.DisposeAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task SignalRStateSubscriber_ShouldReceiveStateUpdates()
    {
        // Arrange
        var hubConnection = CreateTestHubConnection("/statehub");
        var subscriber = new SignalRStateSubscriber(hubConnection);
        var receivedStates = CreateStateCollection();

        // Subscribe to state updates
        subscriber.Subscribe(state => receivedStates.Enqueue(state));

        // Act
        await subscriber.ConnectAsync();

        // Wait for some updates (1 second should give ~10-11 updates at 93ms intervals)
        await Task.Delay(1000);

        // Assert
        var snapshot = receivedStates.ToList();
        snapshot.Should().HaveCountGreaterThan(8, "simulator sends ~10-11 packets per second at 93ms");
        snapshot.Should().HaveCountLessThan(13, "some packets may be missed due to timing/network overhead");

        // Verify timestamps are recent and increasing
        snapshot.Should().OnlyContain(s => s.Timestamp > DateTime.UtcNow.AddSeconds(-2));

        var timestamps = snapshot.Select(s => s.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder("timestamps should be monotonically increasing");

        // Cleanup
        await subscriber.DisposeAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task Multiple_SignalRStateSubscribers_ShouldReceiveStateUpdates()
    {
        // Arrange
        var hubConnection1 = CreateTestHubConnection("/statehub");
        var hubConnection2 = CreateTestHubConnection("/statehub");
        var hubConnection3 = CreateTestHubConnection("/statehub");

        var subscriber1 = new SignalRStateSubscriber(hubConnection1);
        var subscriber2 = new SignalRStateSubscriber(hubConnection2);
        var subscriber3 = new SignalRStateSubscriber(hubConnection3);

        var count1 = 0;
        var count2 = 0;
        var count3 = 0;

        subscriber1.Subscribe(s => count1++);
        subscriber2.Subscribe(s => count2++);
        subscriber3.Subscribe(s => count3++);

        // Act
        await subscriber1.ConnectAsync();
        await subscriber2.ConnectAsync();
        await subscriber3.ConnectAsync();

        await Task.Delay(1000);

        // Assert - All subscribers should receive updates
        // Note: Exact counts may vary in integration tests due to timing, buffering, and concurrency
        count1.Should().BeGreaterThan(0, "subscriber 1 should receive updates");
        count2.Should().BeGreaterThan(0, "subscriber 2 should receive updates");
        count3.Should().BeGreaterThan(0, "subscriber 3 should receive updates");

        // Cleanup
        await subscriber1.DisposeAsync();
        await subscriber2.DisposeAsync();
        await subscriber3.DisposeAsync();
        await hubConnection1.DisposeAsync();
        await hubConnection2.DisposeAsync();
        await hubConnection3.DisposeAsync();
    }
}
