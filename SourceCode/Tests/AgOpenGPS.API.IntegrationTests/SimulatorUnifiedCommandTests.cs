using System.Collections.Concurrent;
using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;
using AgOpenGPS.API.IntegrationTests.Common;
using FluentAssertions;

namespace AgOpenGPS.API.IntegrationTests;

/// <summary>
/// Integration tests for unified UpdateSimulatorCommand with event object pattern.
/// Tests all event types, smooth transitions, and complex scenarios.
/// Refactored to use synchronous tick advancement (TestSimulatorTimer) instead of delays/polling.
/// </summary>
[TestFixture]
public class SimulatorUnifiedCommandTests : BaseIntegrationTest
{
    protected override int TestFixturePort => 15559;

    private IBackendClient? _backendClient;
    private readonly ConcurrentQueue<ApplicationState> _receivedStates = CreateStateCollection();

    [SetUp]
    public async Task SetUp()
    {
        _receivedStates.Clear();

        var hubConnection = CreateTestHubConnection("/statehub");
        _backendClient = new SignalRBackendClient(hubConnection, new SignalRCommandRouter());
        await _backendClient.ConnectAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_backendClient != null && _backendClient.IsConnected)
        {
            try
            {
                await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
                    SimulatorEvent.Stop()));
            }
            catch
            {
                // Ignore cleanup errors
            }

            await _backendClient.DisposeAsync();
        }

        _receivedStates.Clear();
    }


    #region Start/Stop Events

    [Test]
    public async Task Start_ShouldProduceExpectedFirstGpsState()
    {
        // Verify: First tick after Start command produces exact physics results
        // Golden values captured from simulator on 2025-11-12
        // Physics: Start (45.0, -93.0), Heading: 0°, Speed: 10 km/h (instant)
        // Tick 1: 10 km/h for 93ms = 0.258m north via great circle navigation

        const double ExpectedLatitude = 45.00000232324749; // Captured golden value
        const double ExpectedLongitude = -93.0;
        const double ExpectedSpeed = 10.0;
        const double ExpectedHeading = 0.0;

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 1)
            {
                semaphore.Release();
            }
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue("should receive GPS data within 5 seconds");

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCount(1, "should receive exactly 1 GPS state after first tick");

        var gps = gpsStates.Single().Gnss!;

        gps.WgsPosition.Latitude.Should().Be(ExpectedLatitude,
            "latitude after first tick moving north at 10 km/h");
        gps.WgsPosition.Longitude.Should().Be(ExpectedLongitude,
            "longitude should not change when moving due north");
        gps.Speed!.KilometersPerHour.Should().BeApproximately(ExpectedSpeed, 0.1,
            "Start command sets speed instantly to 10 km/h");
        gps.HeadingSingle!.Degrees.Should().BeApproximately(ExpectedHeading, 0.1,
            "heading should be 0° (north) as commanded");

        Console.WriteLine($"✓ Start: Lat={gps.WgsPosition.Latitude}, Lon={gps.WgsPosition.Longitude}, Speed={gps.Speed.KilometersPerHour}, Heading={gps.HeadingSingle.Degrees}");
    }

    #endregion

    #region Speed Events

    [Test]
    public async Task SpeedSet_ShouldChangeInstantly()
    {
        // Verify: SpeedSet changes speed instantly to target (no acceleration)
        // Start: 10 km/h → SpeedSet(15) → immediately 15 km/h on first tick

        const double InitialSpeed = 10.0;
        const double TargetSpeed = 15.0;
        const double ExpectedSpeed = 15.0;

        // Arrange - Start at 10 km/h
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 1)
            {
                semaphore.Release();
            }
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

        _receivedStates.Clear(); // Clear start data

        // Act - Set speed instantly to 15 km/h
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedSet(new Speed(TargetSpeed))));

        Factory.TestTimer.AdvanceTick();
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - Speed changed instantly
        var gps = _receivedStates.Single(s => s.Gnss != null).Gnss!;
        gps.Speed!.KilometersPerHour.Should().BeApproximately(ExpectedSpeed, 0.1,
            "SpeedSet should change speed instantly without acceleration");

        Console.WriteLine($"✓ SpeedSet: instant change to {gps.Speed.KilometersPerHour:F1} km/h");
    }

    [Test]
    public async Task SpeedAdjust_ShouldAccelerateGradually()
    {
        // Verify: SpeedAdjust uses physics acceleration to reach target smoothly
        // Golden values: Start 0 km/h, adjust to 10 km/h, reaches target after ~12 ticks
        // Physics: 0.86 km/h per tick acceleration rate
        // Expected: 0 → 0.86 → 1.72 → 2.58 → ... → 10.0 (in ~12 ticks)

        const double InitialSpeed = 0.0;
        const double TargetSpeed = 10.0;
        const int ExpectedTicksToReach = 12; // Ceil(10.0 / 0.86) ≈ 12
        const double ExpectedFinalSpeed = 10.0;

        // Arrange - Start at zero speed
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= ExpectedTicksToReach)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

        _receivedStates.Clear(); // Clear start data

        // Act - Adjust speed to 10 km/h (smooth acceleration)
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(TargetSpeed)));

        Factory.TestTimer.AdvanceTick(); // Start the tick advancement cycle

        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - Speed reached target after ~12 ticks
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        var finalSpeed = gpsStates.Last().Gnss!.Speed!.KilometersPerHour;

        finalSpeed.Should().BeApproximately(ExpectedFinalSpeed, 0.1,
            "SpeedAdjust should reach target speed after gradual acceleration");

        // Verify intermediate speeds show gradual increase (not instant)
        var speeds = gpsStates.Take(3).Select(s => s.Gnss!.Speed!.KilometersPerHour).ToList();
        speeds[0].Should().BeLessThan(speeds[1], "speed should increase gradually tick by tick");
        speeds[1].Should().BeLessThan(speeds[2], "speed should continue increasing");

        Console.WriteLine($"✓ SpeedAdjust: gradual acceleration 0 → {finalSpeed:F1} km/h over {gpsStates.Count} ticks");
    }

    #endregion

    #region Steering Events

    [Test]
    public async Task SteeringSet_ShouldApplySteeringAngle()
    {
        // Verify: SteeringSet applies specific steering angle and changes heading
        // Golden values: Start heading 0° (north), 10 km/h, set steering to 20° right
        // After 3 ticks, heading should change based on Ackermann steering geometry

        const double InitialSpeed = 10.0;
        const double InitialHeading = 0.0;
        const double SteeringAngleDegrees = 20.0;
        const int TickCount = 3;

        // Arrange - Start moving north at 10 km/h
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= TickCount)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(InitialHeading), new Speed(InitialSpeed))));

        _receivedStates.Clear(); // Clear start data

        // Act - Apply steering angle
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(SteeringAngleDegrees))));

        Factory.TestTimer.AdvanceTick();
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - Steering applied and heading changed
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCount(TickCount, $"should receive exactly {TickCount} GPS states");

        var finalHeading = gpsStates.Last().Gnss!.HeadingSingle!.Degrees;

        finalHeading.Should().BeGreaterThan(InitialHeading,
            "heading should increase when steering right (positive angle)");

        Console.WriteLine($"✓ SteeringSet: heading changed from {InitialHeading:F1}° to {finalHeading:F1}° after {TickCount} ticks");
    }

    [Test]
    public async Task SteeringReset_ShouldResetToZero()
    {
        // Verify: SteeringReset resets steering to 0° (straight) after being set to non-zero
        // Start with 20° steering, then reset, should return to 0° (centered)
        // After reset, heading changes should be minimal indicating straight path

        const double InitialSpeed = 10.0;
        const double InitialSteering = 20.0;
        const int TickCount = 3;

        // Arrange - Start with steering applied
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= TickCount)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(InitialSteering))));

        _receivedStates.Clear(); // Clear setup data

        // Act - Reset steering to zero
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringReset()));

        Factory.TestTimer.AdvanceTick();
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - Steering reset causes straighter path (minimal heading changes)
        var headings = _receivedStates.Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .ToList();

        headings.Should().HaveCount(TickCount, $"should receive exactly {TickCount} GPS states");

        // Heading variance should be small indicating straight path
        var headingVariance = headings.Max() - headings.Min();
        headingVariance.Should().BeLessThan(5.0, "path should be nearly straight after reset");

        Console.WriteLine($"✓ SteeringReset: path straightened (variance={headingVariance:F1}° over {TickCount} ticks)");
    }

    #endregion

    #region Direction Events

    [Test]
    public async Task DirectionReverse_ShouldFlip180Degrees()
    {
        // Verify: DirectionReverse flips heading by exactly 180°
        // Golden values: Start heading 0° (north), reverse should give 180° (south)

        const double InitialHeading = 0.0;
        const double ExpectedHeadingAfterReverse = 180.0;

        // Arrange - Start moving north
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 1)
            {
                semaphore.Release();
            }
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(InitialHeading), new Speed(10.0))));

        _receivedStates.Clear(); // Clear start data

        // Act - Reverse direction
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));

        Factory.TestTimer.AdvanceTick();
        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - Heading flipped 180°
        var gps = _receivedStates.Single(s => s.Gnss != null).Gnss!;

        gps.HeadingSingle!.Degrees.Should().BeApproximately(ExpectedHeadingAfterReverse, 0.1,
            "DirectionReverse should flip heading by exactly 180° (north → south)");

        Console.WriteLine($"✓ DirectionReverse: {InitialHeading:F1}° → {gps.HeadingSingle.Degrees:F1}°");
    }

    #endregion

    #region Reset Events

    [Test]
    public async Task PositionReset_ShouldReturnToStartPosition()
    {
        // Verify: PositionReset returns to initial position after movement
        // Golden values: Start at (45.0, -93.0), move north, reset should return to (45.0, -93.0)

        const double InitialLatitude = 45.0;
        const double InitialLongitude = -93.0;
        const int MovementTicks = 3;

        // Phase 1: Start and move vehicle
        var movementSemaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= MovementTicks)
            {
                movementSemaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(InitialLatitude, InitialLongitude), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick(); // Start movement

        var acquired1 = await movementSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired1.Should().BeTrue("should complete movement phase");

        // Assert movement occurred
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCount(MovementTicks, $"should receive {MovementTicks} GPS states during movement");

        var lastPosition = gpsStates.Last().Gnss!;
        lastPosition.WgsPosition.Latitude.Should().NotBe(InitialLatitude,
            "latitude should have changed after movement (verifying movement occurred before reset)");

        // Phase 2: Reset position
        _receivedStates.Clear(); // Clear movement data

        var resetSemaphore = new SemaphoreSlim(0, 1);
        _backendClient.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 1)
            {
                resetSemaphore.Release();
            }
        });

        // Act - Reset position
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.PositionReset()));

        Factory.TestTimer.AdvanceTick();
        var acquired2 = await resetSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired2.Should().BeTrue("should complete reset phase");

        // Assert - Position reset to start
        var resetStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        resetStates.Should().HaveCountGreaterThanOrEqualTo(1, "should receive at least one GPS state after reset");

        var gps = resetStates.First().Gnss!;

        gps.WgsPosition.Latitude.Should().BeApproximately(InitialLatitude, 0.001,
            "PositionReset should return latitude to initial start position");
        gps.WgsPosition.Longitude.Should().BeApproximately(InitialLongitude, 0.001,
            "PositionReset should return longitude to initial start position");

        Console.WriteLine($"✓ PositionReset: returned to ({gps.WgsPosition.Latitude}, {gps.WgsPosition.Longitude})");
    }

    #endregion

    #region Multi-Client Tests

    [Test]
    public async Task MultipleClients_ShouldReceiveSameState()
    {
        // Verify: Multiple SignalR clients receive identical state updates
        // Golden values captured from simulator on 2025-11-12

        const double ExpectedLatitude = 45.00000232324749; // Captured golden value
        const double ExpectedLongitude = -93.0;
        const double ExpectedSpeed = 10.0;
        const double ExpectedHeading = 0.0;

        // Arrange - Create second client
        var hubConnection2 = CreateTestHubConnection("/statehub");
        var backendClient2 = new SignalRBackendClient(hubConnection2, new SignalRCommandRouter());
        var receivedStates2 = new ConcurrentQueue<ApplicationState>();
        var semaphore1 = new SemaphoreSlim(0, 1);
        var semaphore2 = new SemaphoreSlim(0, 1);

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 1)
            {
                semaphore1.Release();
            }
        });

        backendClient2.SubscribeToState(onNext: state =>
        {
            receivedStates2.Enqueue(state);
            if (receivedStates2.Count(s => s.Gnss != null) >= 1)
            {
                semaphore2.Release();
            }
        });

        await backendClient2.ConnectAsync();

        // Act - Start simulator
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();

        var acquired1 = await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
        var acquired2 = await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

        acquired1.Should().BeTrue("first client should receive state");
        acquired2.Should().BeTrue("second client should receive state");

        // Assert - Both clients received identical data
        var gps1 = _receivedStates.Single(s => s.Gnss != null).Gnss!;
        var gps2 = receivedStates2.Single(s => s.Gnss != null).Gnss!;

        gps1.WgsPosition.Latitude.Should().Be(ExpectedLatitude);
        gps2.WgsPosition.Latitude.Should().Be(ExpectedLatitude,
            "both clients should receive identical latitude");

        gps1.WgsPosition.Longitude.Should().Be(ExpectedLongitude);
        gps2.WgsPosition.Longitude.Should().Be(ExpectedLongitude,
            "both clients should receive identical longitude");

        gps1.Speed!.KilometersPerHour.Should().BeApproximately(ExpectedSpeed, 0.1);
        gps2.Speed!.KilometersPerHour.Should().BeApproximately(ExpectedSpeed, 0.1,
            "both clients should receive identical speed");

        gps1.HeadingSingle!.Degrees.Should().BeApproximately(ExpectedHeading, 0.1);
        gps2.HeadingSingle!.Degrees.Should().BeApproximately(ExpectedHeading, 0.1,
            "both clients should receive identical heading");

        Console.WriteLine($"✓ Multi-client broadcast: both received identical state (Lat={gps1.WgsPosition.Latitude})");

        // Cleanup
        await backendClient2.DisposeAsync();
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task ZeroSpeed_ShouldNotMove()
    {
        // Verify: Vehicle at speed 0 does not change position over multiple ticks
        // Golden values: Position should remain exactly (45.0, -93.0) after 5 ticks

        const double InitialLatitude = 45.0;
        const double InitialLongitude = -93.0;
        const double ExpectedLatitude = 45.0;
        const double ExpectedLongitude = -93.0;
        const int TickCount = 5;

        // Arrange - Start at zero speed
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= TickCount)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(InitialLatitude, InitialLongitude), new Heading(0.0), new Speed(0.0))));

        // Act - Start tick advancement cycle
        Factory.TestTimer.AdvanceTick();

        var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        // Assert - All positions should be identical (no movement)
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).Take(TickCount).ToList();
        gpsStates.Should().HaveCount(TickCount, $"should receive exactly {TickCount} GPS states");

        foreach (var state in gpsStates)
        {
            var gps = state.Gnss!;
            gps.WgsPosition.Latitude.Should().BeApproximately(ExpectedLatitude, 0.000001,
                "latitude should not change at zero speed");
            gps.WgsPosition.Longitude.Should().BeApproximately(ExpectedLongitude, 0.000001,
                "longitude should not change at zero speed");
            gps.Speed!.KilometersPerHour.Should().BeApproximately(0.0, 0.001,
                "speed should remain zero");
        }

        Console.WriteLine($"✓ Zero speed verified: position stable ({ExpectedLatitude}, {ExpectedLongitude}) over {TickCount} ticks");
    }

    #endregion
}
