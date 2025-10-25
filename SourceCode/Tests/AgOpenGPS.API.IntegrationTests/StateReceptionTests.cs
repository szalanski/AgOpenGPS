using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;
using AgOpenGPS.API.IntegrationTests.Common;
using FluentAssertions;

namespace AgOpenGPS.API.IntegrationTests;

/// <summary>
/// Integration tests for state reception via SignalR.
/// Validates that clients successfully receive ApplicationState updates from the backend.
/// </summary>
[TestFixture]
public class StateReceptionTests : BaseIntegrationTest
{
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
        await subscriber.DisconnectAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task SignalRStateSubscriber_ShouldReceiveStateUpdates()
    {
        // Arrange
        var hubConnection = CreateTestHubConnection("/statehub");
        var subscriber = new SignalRStateSubscriber(hubConnection);
        var receivedStates = new List<ApplicationState>();

        // Subscribe to state updates
        var subscription = subscriber.Subscribe(state => receivedStates.Add(state));

        // Act
        await subscriber.ConnectAsync();

        // Wait for some updates (1 second should give ~10 updates at 10 Hz)
        await Task.Delay(1000);

        // Assert
        receivedStates.Should().HaveCountGreaterThan(8, "at least 8 updates should arrive in 1 second at 10 Hz");
        receivedStates.Should().HaveCountLessThan(12, "at most 12 updates should arrive in 1 second at 10 Hz");

        // Verify timestamps are recent and increasing
        receivedStates.Should().OnlyContain(s => s.Timestamp > DateTime.UtcNow.AddSeconds(-2));

        var timestamps = receivedStates.Select(s => s.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder("timestamps should be monotonically increasing");

        // Cleanup
        subscription.Dispose();
        await subscriber.DisconnectAsync();
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

        var subscription1 = subscriber1.Subscribe(s => count1++);
        var subscription2 = subscriber2.Subscribe(s => count2++);
        var subscription3 = subscriber3.Subscribe(s => count3++);

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
        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
        await subscriber1.DisconnectAsync();
        await subscriber2.DisconnectAsync();
        await subscriber3.DisconnectAsync();
        await hubConnection1.DisposeAsync();
        await hubConnection2.DisposeAsync();
        await hubConnection3.DisposeAsync();
    }
}
