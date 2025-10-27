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
/// </summary>
[TestFixture]
public class SimulatorUnifiedCommandTests : BaseIntegrationTest
{
    private IBackendClient? _backendClient;
    private readonly ConcurrentQueue<ApplicationState> _receivedStates = CreateStateCollection();

    [SetUp]
    public async Task SetUp()
    {
        _receivedStates.Clear();

        var hubConnection = CreateTestHubConnection("/statehub");
        _backendClient = new SignalRBackendClient(hubConnection);

        _backendClient.SubscribeToState(
            onNext: state => _receivedStates.Enqueue(state),
            onError: ex => Console.WriteLine($"State subscription error: {ex.Message}")
        );

        await _backendClient.ConnectAsync();
        await Task.Delay(100);
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
                await Task.Delay(200);
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
        // Arrange & Act
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

        await Task.Delay(2000);

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().NotBeEmpty("simulator should generate GPS data");
        gpsStates.Should().HaveCountGreaterThan(10, "simulator should send multiple packets");

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
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));
        await Task.Delay(1000);

        var countBeforeStop = _receivedStates.ToList().Count(s => s.Gnss != null);
        countBeforeStop.Should().BeGreaterThan(5);

        // Act - Stop simulator
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Stop()));
        await Task.Delay(500);

        _receivedStates.Clear();
        await Task.Delay(1000);

        // Assert - No new GPS data
        var countAfterStop = _receivedStates.ToList().Count(s => s.Gnss != null);
        countAfterStop.Should().Be(0, "no GPS data should be generated after stop");

        Console.WriteLine($"✓ Stop event: simulator disabled successfully");
    }

    [Test]
    public async Task UpdateSimulator_StartWithoutData_ShouldLogWarningAndSkip()
    {
        // Arrange - Create event without StartData (manually, not using factory)
        var invalidEvent = new SimulatorEvent { Type = SimulatorEventType.Start, StartData = null };

        // Act
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(invalidEvent));
        await Task.Delay(1000);

        // Assert - No GPS data should be generated
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().BeEmpty("invalid Start event should be ignored");

        Console.WriteLine($"✓ Start without data: properly skipped");
    }

    #endregion

    #region Speed Events

    [Test]
    public async Task UpdateSimulator_SpeedAdjustPositive_ShouldIncrease()
    {
        // Arrange - Start at 5 km/h
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));
        await Task.Delay(1000);

        var initialSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Adjust speed up by 2 km/h, 5 times
        for (int i = 0; i < 5; i++)
        {
            await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
                SimulatorEvent.SpeedAdjust(2.0)));
            await Task.Delay(200);
        }

        await Task.Delay(2000); // Wait for smooth transition

        // Assert - Speed should have increased
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeGreaterThan(initialSpeed);
        finalSpeed.Should().BeApproximately(15.0, 2.0, "speed should increase by ~10 km/h");

        Console.WriteLine($"✓ SpeedAdjust: {initialSpeed:F1} → {finalSpeed:F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedAdjustNegative_ShouldDecrease()
    {
        // Arrange - Start at 20 km/h
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(20.0))));
        await Task.Delay(1000);

        var initialSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Adjust speed down
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(-10.0)));
        await Task.Delay(3000); // Wait for smooth transition

        // Assert
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeLessThan(initialSpeed);
        finalSpeed.Should().BeApproximately(10.0, 2.0);

        Console.WriteLine($"✓ SpeedAdjust negative: {initialSpeed:F1} → {finalSpeed:F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedAdjustWithoutValue_ShouldSkip()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));
        await Task.Delay(1000);

        var initialSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        // Act - Send invalid event (no SpeedDelta)
        var invalidEvent = new SimulatorEvent { Type = SimulatorEventType.SpeedAdjust, SpeedDelta = null };
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(invalidEvent));
        await Task.Delay(1500);

        // Assert - Speed should be unchanged
        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        finalSpeed.Should().BeApproximately(initialSpeed, 1.0, "speed should not change");

        Console.WriteLine($"✓ SpeedAdjust without value: properly skipped");
    }

    [Test]
    public async Task UpdateSimulator_SpeedSetSmooth_ShouldTransitionGradually()
    {
        // Arrange - Start at 5 km/h
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));
        await Task.Delay(1000);
        _receivedStates.Clear();

        // Act - Set speed to 25 km/h with smooth transition
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedSetSmooth(new Speed(25.0))));

        // Collect speeds over time
        await Task.Delay(3000);

        // Assert - Verify gradual transition
        var speeds = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .ToList();

        speeds.Should().NotBeEmpty();

        // Should have intermediate speeds (not instant jump)
        var minSpeed = speeds.Min();
        var maxSpeed = speeds.Max();

        minSpeed.Should().BeLessThan(15.0, "should start from low speed");
        maxSpeed.Should().BeGreaterThan(20.0, "should reach target speed");

        Console.WriteLine($"✓ SpeedSetSmooth: gradual transition from {minSpeed:F1} to {maxSpeed:F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedSet_ShouldChangeInstantly()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));
        await Task.Delay(1000);
        _receivedStates.Clear();

        // Act - Set speed instantly
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedSet(new Speed(30.0))));
        await Task.Delay(500); // Short delay

        // Assert - Speed should change quickly
        var speeds = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .ToList();

        speeds.Should().NotBeEmpty();
        speeds.Last().Should().BeApproximately(30.0, 2.0, "speed should reach target quickly");

        Console.WriteLine($"✓ SpeedSet: instant change to {speeds.Last():F1} km/h");
    }

    [Test]
    public async Task UpdateSimulator_SpeedZero_ShouldStopImmediately()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(25.0))));
        await Task.Delay(1000);
        _receivedStates.Clear();

        // Act - Instant stop
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedZero()));
        await Task.Delay(500);

        // Assert
        var speeds = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .ToList();

        speeds.Should().NotBeEmpty();
        speeds.Last().Should().BeApproximately(0.0, 0.5, "speed should be zero");

        Console.WriteLine($"✓ SpeedZero: instant stop");
    }

    [Test]
    public async Task UpdateSimulator_SpeedAdjustBeyondLimits_ShouldClamp()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(0.0))));
        await Task.Delay(1000);

        // Act - Try to exceed max speed (322 km/h)
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(500.0)));
        await Task.Delay(3000);

        // Assert - Speed should clamp at 322 km/h
        var speed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        speed.Should().BeLessThanOrEqualTo(322.0, "speed should clamp at max");

        Console.WriteLine($"✓ Speed clamping: capped at {speed:F1} km/h");
    }

    #endregion

    #region Steering Events

    [Test]
    public async Task UpdateSimulator_SteeringSet_ShouldChangeAngle()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));
        await Task.Delay(1000);

        var initialHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .FirstOrDefault();

        _receivedStates.Clear();

        // Act - Apply steering
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(20.0))));
        await Task.Delay(2500);

        // Assert - Heading should have changed
        var finalHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        var headingChange = Math.Abs(finalHeading - initialHeading);
        headingChange.Should().BeGreaterThan(5.0, "heading should change with steering applied");

        Console.WriteLine($"✓ SteeringSet: heading changed from {initialHeading:F1}° to {finalHeading:F1}°");
    }

    [Test]
    public async Task UpdateSimulator_SteeringReset_ShouldCenterSteering()
    {
        // Arrange - Apply steering first
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(25.0))));
        await Task.Delay(1500);

        var headingBefore = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Reset steering
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringReset()));
        await Task.Delay(2000);

        // Assert - Path should straighten
        var headingsAfter = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .ToList();

        headingsAfter.Should().NotBeEmpty();
        // Heading changes should be minimal (straightened path)
        var headingVariance = headingsAfter.Max() - headingsAfter.Min();
        headingVariance.Should().BeLessThan(10.0, "path should straighten after reset");

        Console.WriteLine($"✓ SteeringReset: path straightened (variance={headingVariance:F1}°)");
    }

    [Test]
    public async Task UpdateSimulator_SteeringWithoutValue_ShouldSkip()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));
        await Task.Delay(1000);

        // Act - Invalid steering event (no SteeringValue)
        var invalidEvent = new SimulatorEvent { Type = SimulatorEventType.SteeringSet, SteeringValue = null };
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(invalidEvent));
        await Task.Delay(500);

        // Assert - Should not crash, just skip
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().NotBeEmpty("simulator should continue running");

        Console.WriteLine($"✓ SteeringSet without value: properly skipped");
    }

    #endregion

    #region Direction Events

    [Test]
    public async Task UpdateSimulator_DirectionReverse_ShouldFlip180Degrees()
    {
        // Arrange - Start heading north (0°)
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));
        await Task.Delay(1500);

        var initialHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .FirstOrDefault();

        // Act - Reverse direction
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));
        await Task.Delay(500);

        // Assert - Should be 180° opposite
        var newHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        var expectedHeading = (initialHeading + 180) % 360;
        newHeading.Should().BeApproximately(expectedHeading, 5.0, "should be 180° opposite");

        Console.WriteLine($"✓ DirectionReverse: {initialHeading:F1}° → {newHeading:F1}°");
    }

    [Test]
    public async Task UpdateSimulator_DirectionReverseMultiple_ShouldToggle()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));
        await Task.Delay(1000);

        var initialHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .FirstOrDefault();

        // Act - Reverse twice (should return to original)
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));
        await Task.Delay(300);
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));
        await Task.Delay(500);

        // Assert - Should be back to original heading
        var finalHeading = _receivedStates.ToList()
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        finalHeading.Should().BeApproximately(initialHeading, 5.0, "should return to original after double reverse");

        Console.WriteLine($"✓ Double reverse: {initialHeading:F1}° → {finalHeading:F1}°");
    }

    [Test]
    public async Task UpdateSimulator_DirectionReverse_ShouldMaintainSpeed()
    {
        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(15.0))));
        await Task.Delay(1000);

        var speedBefore = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        // Act - Reverse direction
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));
        await Task.Delay(1000);

        // Assert - Speed unchanged
        var speedAfter = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();

        speedAfter.Should().BeApproximately(speedBefore, 1.0, "speed should not change");

        Console.WriteLine($"✓ DirectionReverse: speed maintained at {speedAfter:F1} km/h");
    }

    #endregion

    #region Reset Events

    [Test]
    public async Task UpdateSimulator_PositionReset_ShouldReturnToStart()
    {
        // Arrange - Start and move
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(20.0))));
        await Task.Delay(2500);

        var movedPosition = _receivedStates.ToList()
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .LastOrDefault();

        movedPosition.Latitude.Should().NotBe(45.0); // Should have moved

        // Act - Reset position
        var resetEvent = new SimulatorEvent
        {
            Type = SimulatorEventType.PositionReset,
            StartData = new SimulatorStartData(
                new Wgs84Position(45.0, -93.0),
                new Heading(0.0),
                new Speed(0.0))
        };
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(resetEvent));
        await Task.Delay(1000);

        // Assert - Back to start
        var resetPosition = _receivedStates.ToList()
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .LastOrDefault();

        resetPosition.Latitude.Should().BeApproximately(45.0, 0.001);
        resetPosition.Longitude.Should().BeApproximately(-93.0, 0.001);

        Console.WriteLine($"✓ PositionReset: returned to start");
    }

    [Test]
    public async Task UpdateSimulator_Reset_ShouldResetPositionOnly()
    {
        // Arrange - Start at initial position and let it move
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(90.0), new Speed(25.0))));
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(30.0))));
        await Task.Delay(2500); // Let it move away

        var movedPos = _receivedStates.ToList()
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .LastOrDefault();

        var distanceMoved = Math.Abs(movedPos.Latitude - 45.0) + Math.Abs(movedPos.Longitude - (-93.0));
        distanceMoved.Should().BeGreaterThan(0.0001, "vehicle should have moved");

        // Act - Reset (legacy: only position, not speed/steering)
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Reset()));
        _receivedStates.Clear();
        await Task.Delay(1500);

        // Assert - Position reset, but speed/steering maintained (legacy behavior)
        var snapshot = _receivedStates.ToList();
        var resetPos = snapshot
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!.WgsPosition)
            .FirstOrDefault();

        resetPos.Latitude.Should().BeApproximately(45.0, 0.001, "latitude reset to initial start");
        resetPos.Longitude.Should().BeApproximately(-93.0, 0.001, "longitude reset to initial start");

        // Speed should be maintained (legacy behavior)
        var finalSpeed = snapshot
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        finalSpeed.Should().BeGreaterThan(15.0, "speed maintained after reset (legacy behavior)");

        Console.WriteLine($"✓ Reset: position reset to (45.0, -93.0), speed maintained at {finalSpeed:F1} km/h");
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public async Task ComplexScenario_RepeatButtonSimulation()
    {
        // Simulate holding speed-up button (multiple SpeedAdjust events)

        // Arrange
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(0.0))));
        await Task.Delay(500);
        _receivedStates.Clear();

        // Act - Simulate 20 button repeats (2 seconds at 100ms intervals)
        for (int i = 0; i < 20; i++)
        {
            await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
                SimulatorEvent.SpeedAdjust(0.5)));
            await Task.Delay(100);
        }

        await Task.Delay(3000); // Wait for smooth transition

        // Assert - Speed should have increased gradually
        var speeds = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .ToList();

        speeds.Should().NotBeEmpty();
        speeds.Last().Should().BeGreaterThan(5.0, "speed should increase from button repeats");

        Console.WriteLine($"✓ Repeat button simulation: reached {speeds.Last():F1} km/h");
    }

    [Test]
    public async Task ComplexScenario_FullUserWorkflow()
    {
        // Full scenario: Start → Speed up → Steer → Reverse → Stop

        // Start
        await _backendClient!.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(5.0))));
        await Task.Delay(1000);

        // Speed up
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedAdjust(10.0)));
        await Task.Delay(2000);

        var speed1 = _receivedStates
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        speed1.Should().BeGreaterThan(10.0);

        // Apply steering
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SteeringSet(new SteeringAngle(15.0))));
        await Task.Delay(1500);

        // Reverse direction
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.DirectionReverse()));
        await Task.Delay(500);

        // Stop
        await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.SpeedZero()));
        await Task.Delay(1000);

        var finalSpeed = _receivedStates.ToList()
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        finalSpeed.Should().BeApproximately(0.0, 0.5, "should end at zero speed");

        Console.WriteLine($"✓ Full workflow completed: {_receivedStates.ToList().Count(s => s.Gnss != null)} GPS states");
    }

    #endregion
}
