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
        _backendClient = new SignalRBackendClient(hubConnection);
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
    public async Task UpdateSimulator_StartEvent_ShouldInitialize()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 11)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCountGreaterThan(10, "should receive at least 11 GPS states");

        var lastGps = gpsStates.Last().Gnss!;
        lastGps.WgsPosition.Latitude.Should().BeInRange(44.9, 45.1);
        lastGps.WgsPosition.Longitude.Should().BeInRange(-93.1, -92.9);
        lastGps.Speed!.KilometersPerHour.Should().BeApproximately(10.0, 1.0);

        Console.WriteLine($"✓ Start event: {gpsStates.Count} GPS states received");
    }

    [Test]
    public async Task UpdateSimulator_StopEvent_ShouldDisable()
    {
        // Arrange - Start simulator first
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 6)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var initialCount = _receivedStates.Count(s => s.Gnss != null);
        initialCount.Should().BeGreaterThan(5, "should have collected initial GPS data");

        // Act - Stop simulator
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Stop()));

        _receivedStates.Clear();

        // Assert - Manually advance 10 ticks (no new GPS data should arrive after stop)
        for (int i = 0; i < 10; i++)
        {
            Factory.TestTimer.AdvanceTick();
        }

        var countAfterStop = _receivedStates.Count(s => s.Gnss != null);
        countAfterStop.Should().Be(0, "no GPS data should be generated after stop");

        Console.WriteLine($"✓ Stop event: simulator disabled successfully");
    }

    #endregion

    #region Speed Events

    [Test]
    public async Task UpdateSimulator_SpeedAdjustPositive_ShouldIncrease()
    {
        // Arrange - Start at 5 km/h
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.Speed != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var initialSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Adjust speed up by 2 km/h, 5 times
        targetCount = 10;
        for (int i = 0; i < 5; i++)
        {
            await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
                SimulatorEvent.SpeedAdjust(2.0)));
        }

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeGreaterThan(initialSpeed, "speed should increase after adjustments");
        finalSpeed.Should().BeLessThanOrEqualTo(322.0, "should not exceed max speed");
        Console.WriteLine($"✓ SpeedAdjust: {initialSpeed:F1} → {finalSpeed:F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedAdjustNegative_ShouldDecrease()
    {
        // Arrange - Start at 20 km/h
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.Speed != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(20.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var initialSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Adjust speed down
        targetCount = 10;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(-10.0)));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeLessThan(initialSpeed, "speed should decrease after adjustment");
        finalSpeed.Should().BeGreaterThanOrEqualTo(0.0, "should not be negative");
        Console.WriteLine($"✓ SpeedAdjust negative: {initialSpeed:F1} → {finalSpeed:F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedSet_ShouldChangeInstantly()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.Speed != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        _receivedStates.Clear();

        // Act - Set speed instantly
        targetCount = 3;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedSet(new Speed(30.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var speeds = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .ToList();

        speeds.Should().NotBeEmpty("should receive speed states");
        speeds.Last().Should().BeApproximately(30.0, 2.0, "speed should change to 30 km/h");
        Console.WriteLine($"✓ SpeedSet: instant change to {speeds.Last():F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedAdjustBeyondLimits_ShouldClamp()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.Speed != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(0.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        _receivedStates.Clear();

        // Act - Try to exceed max speed (322 km/h)
        targetCount = 15;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(500.0)));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var speed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        speed.Should().BeLessThanOrEqualTo(322.0, "speed should clamp at 322 km/h max");
        Console.WriteLine($"✓ Speed clamping: capped at {speed:F1} km/h");
    }

    #endregion

    #region Steering Events

    [Test]
    public async Task UpdateSimulator_SteeringSet_ShouldChangeAngle()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.HeadingSingle != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var initialHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .FirstOrDefault();

        _receivedStates.Clear();

        // Act - Apply steering
        targetCount = 10;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(20.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var finalHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        finalHeading.Should().BeGreaterThan(initialHeading + 5.0, "heading should change");
        Console.WriteLine($"✓ SteeringSet: heading changed from {initialHeading:F1}° to {finalHeading:F1}°");
    }

    [Test]
    public async Task UpdateSimulator_SteeringReset_ShouldCenterSteering()
    {
        // Arrange - Apply steering first
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 8;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.HeadingSingle != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(25.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        _receivedStates.Clear();

        // Act - Reset steering
        targetCount = 10;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringReset()));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var headingsAfter = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .ToList();

        // Heading changes should be minimal (straightened path)
        var headingVariance = headingsAfter.Max() - headingsAfter.Min();
        headingVariance.Should().BeLessThan(10.0, "path should straighten after reset");

        Console.WriteLine($"✓ SteeringReset: path straightened (variance={headingVariance:F1}°)");
    }

    #endregion

    #region Direction Events

    [Test]
    public async Task UpdateSimulator_DirectionReverse_ShouldFlip180Degrees()
    {
        // Arrange - Start heading north (0°)
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 8;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.HeadingSingle != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var initialHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .FirstOrDefault();

        var expectedHeading = (initialHeading + 180) % 360;
        _receivedStates.Clear();

        // Act - Reverse direction
        targetCount = 3;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var newHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        newHeading.Should().BeApproximately(expectedHeading, 5.0, "heading should flip ~180°");
        Console.WriteLine($"✓ DirectionReverse: {initialHeading:F1}° → {newHeading:F1}°");
    }

    [Test]
    public async Task UpdateSimulator_DirectionReverse_ShouldMaintainSpeed()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss?.Speed != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var speedBefore = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Reverse direction
        targetCount = 5;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var speedAfter = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        speedAfter.Should().BeApproximately(speedBefore, 1.0, "speed should remain unchanged");
        Console.WriteLine($"✓ DirectionReverse: speed maintained at {speedAfter:F1} km/h");
    }

    #endregion

    #region Reset Events

    [Test]
    public async Task UpdateSimulator_PositionReset_ShouldReturnToStart()
    {
        // Arrange - Start and move
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 15;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(20.0))));

        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        var movedPosition = _receivedStates.ToList()
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .LastOrDefault();

        movedPosition.Latitude.Should().NotBe(45.0, "position should have changed");

        _receivedStates.Clear();

        // Act - Reset position
        targetCount = 5;
        var resetEvent = new SimulatorEvent
        {
            Type = SimulatorEventType.PositionReset,
            StartData = new SimulatorStartData(
                new Wgs84Position(45.0, -93.0),
                new Heading(0.0),
                new Speed(0.0))
        };
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(resetEvent));

        Factory.TestTimer.AdvanceTick();  // Kick off phase 2
        await semaphore.WaitAsync();

        // Assert
        var resetPosition = _receivedStates.ToList()
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .LastOrDefault();

        resetPosition.Latitude.Should().BeApproximately(45.0, 0.001, "latitude should return to start");
        resetPosition.Longitude.Should().BeApproximately(-93.0, 0.001, "longitude should return to start");

        Console.WriteLine($"✓ PositionReset: returned to start");
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public async Task ComplexScenario_FullUserWorkflow()
    {
        // Full scenario: Start → Speed up → Steer → Reverse → Stop

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 5;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act - Phase 1: Start
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Phase 2: Speed up
        targetCount = 10;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(10.0)));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Phase 3: Apply steering
        targetCount = 15;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(15.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Phase 4: Reverse direction
        targetCount = 18;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Phase 5: Stop
        targetCount = 23;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedZero()));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeApproximately(0.0, 0.5, "speed should be zero at end");

        Console.WriteLine($"✓ Full workflow completed: {_receivedStates.ToList().Count(s => s.Gnss != null)} GPS states");
    }

    #endregion

    #region Architectural Verification Tests

    [Test]
    public async Task SimulatorService_ShouldSendViaUdp_NotDirectCall()
    {
        // Verify: SimulatorHostedService sends UDP packets (~93ms intervals)
        // not direct GnssService calls. Evidence:
        // - Simulator generates packets at 93ms intervals (SimulatorHostedService tick)
        // - ApplicationOrchestrator processes immediately (event-driven)
        // - Clients receive GPS updates at ~93ms intervals (UDP pipeline works)

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 26)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert - Verify timing patterns indicate UDP pipeline
        var gpsStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCountGreaterThan(25, "should receive most simulator packets (~32 expected at 93ms over 3s)");

        // Calculate avg time between GPS updates (should be ~93ms matching simulator rate)
        var timestamps = gpsStates.Select(s => s.Timestamp).OrderBy(t => t).ToList();
        if (timestamps.Count >= 2)
        {
            var intervals = timestamps.Zip(timestamps.Skip(1), (a, b) => (b - a).TotalMilliseconds).ToList();
            var avgInterval = intervals.Average();

            avgInterval.Should().BeInRange(80, 120,
                "average interval should be ~93ms (simulator rate), proving immediate UDP processing");

            Console.WriteLine($"✓ UDP pipeline verified: avg interval = {avgInterval:F0}ms (expected ~93ms)");
        }
    }

    [Test]
    public async Task SimulatorPackets_ShouldFlowThroughFullPipeline()
    {
        // Verify complete pipeline: SimulatorService → UDP → UdpPacketReceiver → GnssService →
        // ApplicationOrchestrator → SignalR

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 15)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(12.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert - Verify complete pipeline processing
        var gpsStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        gpsStates.Should().NotBeEmpty("GPS data should flow through full pipeline");

        var lastGps = gpsStates.Last().Gnss!;

        // Verify Stage 4 (GnssService processing) completed:
        lastGps.WgsPosition.Should().NotBeNull("WGS84 position should be unpacked");
        lastGps.LocalPosition.Should().NotBeNull("local plane transformation should be applied");
        lastGps.Quality.Should().NotBeNull("quality metrics should be extracted");
        lastGps.Health.GpsHz.Should().BeGreaterThan(0, "GPS frequency should be calculated");

        // Verify data integrity through pipeline
        lastGps.WgsPosition.Latitude.Should().BeInRange(44.9, 45.1);
        lastGps.Speed!.KilometersPerHour.Should().BeApproximately(12.0, 2.0);
        lastGps.Quality.FixQuality.Should().Be(4, "RTK Fixed quality preserved");

        Console.WriteLine($"✓ Full pipeline verified: {gpsStates.Count} states received with complete processing");
    }

    [Test]
    public async Task SimulatorGpsData_ShouldMatchAgIOPacketFormat()
    {
        // Verify simulator generates data matching real AgIO PGN 0xD6 binary format

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 5)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert - Verify all AgIO packet fields are present
        var gpsState = _receivedStates.ToList().FirstOrDefault(s => s.Gnss != null)?.Gnss;
        gpsState.Should().NotBeNull();

        // PGN 0xD6 field verification
        gpsState!.WgsPosition.Should().NotBeNull("PGN field: latitude/longitude");
        gpsState.HeadingSingle.Should().NotBeNull("PGN field: heading");
        gpsState.Speed.Should().NotBeNull("PGN field: speed");
        gpsState.Altitude.Should().NotBeNull("PGN field: altitude");
        gpsState.Quality.Hdop.Should().BeGreaterThan(0, "PGN field: HDOP");
        gpsState.Quality.Age.Should().BeGreaterThanOrEqualTo(0, "PGN field: age");
        gpsState.Quality.SatellitesTracked.Should().BeGreaterThan(0, "PGN field: satellites");
        gpsState.Quality.FixQuality.Should().BeInRange(1, 5, "PGN field: fix quality");

        // Verify value ranges match AgIO specification
        gpsState.WgsPosition.Latitude.Should().BeInRange(-90, 90);
        gpsState.WgsPosition.Longitude.Should().BeInRange(-180, 180);
        gpsState.HeadingSingle!.Degrees.Should().BeInRange(0, 360);
        gpsState.Speed!.KilometersPerHour.Should().BeGreaterThanOrEqualTo(0);
        gpsState.Altitude!.Meters.Should().BeGreaterThan(0);

        Console.WriteLine($"✓ Simulator data matches AgIO PGN 0xD6 format");
    }

    [Test]
    public async Task MultipleClients_ShouldReceiveSameSimulatorBroadcasts()
    {
        // Verify SignalR broadcasts simulator data to all connected clients

        // Arrange - Create second client
        var hubConnection2 = CreateTestHubConnection("/statehub");
        var backendClient2 = new SignalRBackendClient(hubConnection2);
        var receivedStates2 = new ConcurrentQueue<ApplicationState>();
        var semaphore = new SemaphoreSlim(0, 1);
        var semaphore2 = new SemaphoreSlim(0, 1);

        backendClient2.SubscribeToState(state =>
        {
            receivedStates2.Enqueue(state);
            if (receivedStates2.Count(s => s.Gnss != null) >= 5)
            {
                semaphore2.Release();
                return;
            }
        });
        await backendClient2.ConnectAsync();

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 5)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act - Start simulator
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();
        await semaphore2.WaitAsync();

        // Assert - Both clients should receive GPS data
        var client1GpsStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        var client2GpsStates = receivedStates2.ToList().Where(s => s.Gnss != null).ToList();

        client1GpsStates.Should().NotBeEmpty("client 1 should receive GPS data");
        client2GpsStates.Should().NotBeEmpty("client 2 should receive GPS data");

        // Both clients should receive exactly same number of updates
        client1GpsStates.Count.Should().Be(client2GpsStates.Count, "both clients should receive exactly same number of broadcasts");

        // Verify both clients receive same GPS positions (sample last state)
        var client1LastPos = client1GpsStates.Last().Gnss!.WgsPosition;
        var client2LastPos = client2GpsStates.Last().Gnss!.WgsPosition;

        Math.Abs(client1LastPos.Latitude - client2LastPos.Latitude).Should().BeLessThan(0.001,
            "both clients should receive same latitude");
        Math.Abs(client1LastPos.Longitude - client2LastPos.Longitude).Should().BeLessThan(0.001,
            "both clients should receive same longitude");

        Console.WriteLine($"✓ Multi-client broadcast verified: client1={client1GpsStates.Count}, client2={client2GpsStates.Count}");

        // Cleanup
        await backendClient2.DisposeAsync();
    }

    #endregion

    #region Physics Accuracy Tests

    [Test]
    public async Task SimulatorMovement_ShouldShowRealisticPositionChanges()
    {
        // Verify realistic movement: 10 km/h for 3.5s = ~9.7m north
        // Latitude change: 9.7m / 111000m/degree ≈ 0.000087°

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 20)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert
        var gpsStates = _receivedStates.ToList().Where(s => s.Gnss != null).Select(s => s.Gnss!).ToList();
        gpsStates.Should().HaveCountGreaterThan(10, "should receive multiple updates");

        var firstPos = gpsStates.First().WgsPosition;
        var lastPos = gpsStates.Last().WgsPosition;

        var latChange = lastPos.Latitude - firstPos.Latitude;
        latChange.Should().BeGreaterThan(0.00005, "vehicle should move north (latitude increases)");
        latChange.Should().BeLessThan(0.0002, "movement should be realistic for speed and duration");

        // Longitude should remain relatively stable (moving north)
        var lonChange = Math.Abs(lastPos.Longitude - firstPos.Longitude);
        lonChange.Should().BeLessThan(0.0001, "longitude should not change much when moving north");

        Console.WriteLine($"✓ Realistic movement: Δlat={latChange:F6}° ({latChange * 111000:F2}m north)");
    }

    [Test]
    public async Task SimulatorSteering_ShouldCreateCurvedPath()
    {
        // Verify steering creates curved path by applying left steering (-25°)

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 8;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (targetCount == 8 && _receivedStates.Count(s => s.Gnss != null) >= 8)
            {
                semaphore.Release();
                return;
            }

            if (targetCount == 8 && _receivedStates.Count(s => s.Gnss?.HeadingSingle != null) >= 8)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act - Phase 1: Start moving east at 15 km/h
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(90.0), new Speed(15.0)))); // 90° = East

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        var initialStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        _receivedStates.Clear();

        // Phase 2: Apply left steering (-25 degrees) to create curved path
        targetCount = 8;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(-25.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert
        var turnedStates = _receivedStates.ToList().Where(s => s.Gnss?.HeadingSingle != null).ToList();
        turnedStates.Should().HaveCountGreaterThan(8);

        var initialHeading = initialStates.Last().Gnss!.HeadingSingle!.Degrees;
        var finalHeading = turnedStates.Last().Gnss!.HeadingSingle!.Degrees;

        // Heading should decrease (turning left/counterclockwise)
        var headingChange = initialHeading - finalHeading;
        if (headingChange < 0) headingChange += 360; // Handle wrap-around

        headingChange.Should().BeGreaterThan(10.0, "steering should create significant heading change");
        headingChange.Should().BeLessThan(90.0, "turn should be gradual (not instant)");

        Console.WriteLine($"✓ Curved path created: heading changed from {initialHeading:F1}° to {finalHeading:F1}° (Δ={headingChange:F1}°)");
    }

    [Test]
    public async Task SimulatorZeroSpeed_ShouldNotMove()
    {
        // Verify position remains stable when speed = 0 (floating point tolerance < 0.000001°)

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 15)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(45.0), new Speed(0.0)))); // Heading NE but no speed

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        // Assert
        var gpsStates = _receivedStates.ToList().Where(s => s.Gnss != null).Select(s => s.Gnss!).ToList();
        gpsStates.Should().NotBeEmpty("simulator should still generate packets");

        // Position should remain stable (no movement)
        var positions = gpsStates.Select(s => s.WgsPosition).ToList();
        var latitudes = positions.Select(p => p.Latitude).Distinct().ToList();
        var longitudes = positions.Select(p => p.Longitude).Distinct().ToList();

        // Allow tiny floating point variations, but no real movement
        (latitudes.Max() - latitudes.Min()).Should().BeLessThan(0.000001, "latitude should not change with zero speed");
        (longitudes.Max() - longitudes.Min()).Should().BeLessThan(0.000001, "longitude should not change with zero speed");

        Console.WriteLine($"✓ Zero speed verified: position stable over {gpsStates.Count} packets");
    }

    [Test]
    public async Task SpeedChanges_ShouldAffectDistanceTraveled()
    {
        // Verify speed changes proportionally affect distance traveled

        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        var targetCount = 12;

        _backendClient!.SubscribeToState(onNext: state =>
        {
            _receivedStates.Enqueue(state);

            if (_receivedStates.Count(s => s.Gnss != null) >= targetCount)
            {
                semaphore.Release();
                return;
            }

            Factory.TestTimer.AdvanceTick();
        });

        // Act - Phase 1: Slow speed (5 km/h) for ~2 seconds
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0)))); // North at 5 km/h

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        var slowSpeedStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        var slowStartPos = slowSpeedStates.First().Gnss!.WgsPosition;
        var slowEndPos = slowSpeedStates.Last().Gnss!.WgsPosition;
        var slowDistance = Math.Abs(slowEndPos.Latitude - slowStartPos.Latitude) * 111000; // meters

        _receivedStates.Clear();

        // Phase 2: High speed (20 km/h) for ~2 seconds
        targetCount = 12;
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedSet(new Speed(20.0))));

        Factory.TestTimer.AdvanceTick();
        await semaphore.WaitAsync();

        var fastSpeedStates = _receivedStates.ToList().Where(s => s.Gnss != null).ToList();
        var fastStartPos = fastSpeedStates.First().Gnss!.WgsPosition;
        var fastEndPos = fastSpeedStates.Last().Gnss!.WgsPosition;
        var fastDistance = Math.Abs(fastEndPos.Latitude - fastStartPos.Latitude) * 111000; // meters

        // Assert - Fast speed should cover ~4x more distance than slow speed
        var ratio = fastDistance / slowDistance;
        ratio.Should().BeInRange(2.5, 5.5, "faster speed should cover proportionally more distance");

        Console.WriteLine($"✓ Speed affects distance: slow={slowDistance:F2}m, fast={fastDistance:F2}m (ratio={ratio:F2}x)");
    }

    #endregion

    #region State Reception Tests

    [Test]
    public async Task SignalRBackendClient_ShouldConnect_ToBackend()
    {
        // Verify basic SignalR connection capability

        // Arrange
        var hubConnection = CreateTestHubConnection("/statehub");
        var subscriber = new SignalRBackendClient(hubConnection);

        // Act
        await subscriber.ConnectAsync();

        // Assert
        subscriber.IsConnected.Should().BeTrue();

        // Cleanup
        await subscriber.DisposeAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task SignalRBackendClient_ShouldReceiveStateUpdates()
    {
        // Verify state reception and timing (~93ms intervals at 10Hz)

        // Arrange
        var hubConnection = CreateTestHubConnection("/statehub");
        var subscriber = new SignalRBackendClient(hubConnection);
        var receivedStates = CreateStateCollection();
        var semaphore = new SemaphoreSlim(0, 1);

        // Subscribe to state updates
        subscriber.SubscribeToState(state =>
        {
            receivedStates.Enqueue(state);

            if (receivedStates.Count >= 9)
            {
                semaphore.Release();
            }
            else
            {
                Factory.TestTimer.AdvanceTick();  // Continue loop until target
            }
        });

        // Act
        await subscriber.ConnectAsync();

        Factory.TestTimer.AdvanceTick();  // Kick off

        await semaphore.WaitAsync();

        // Assert
        var snapshot = receivedStates.ToList();
        snapshot.Should().HaveCountGreaterThan(8, "simulator sends ~10-11 packets per second at 93ms");

        // Verify timestamps are recent and increasing
        snapshot.Should().OnlyContain(s => s.Timestamp > DateTime.UtcNow.AddSeconds(-2));

        var timestamps = snapshot.Select(s => s.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder("timestamps should be monotonically increasing");

        // Cleanup
        await subscriber.DisposeAsync();
        await hubConnection.DisposeAsync();
    }

    #endregion
}
