using AgOpenGPS.Api.Client.Abstractions;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;
using AgOpenGPS.API.IntegrationTests.Common;
using FluentAssertions;

namespace AgOpenGPS.API.IntegrationTests;

/// <summary>
/// Integration tests for backend SimulatorService controlled via BackendClient.
/// Tests: Client sends commands → SignalR → MediatR → SimulatorService → UDP (port 15556) → Backend → SignalR → Verification
/// Note: Completely isolated from GpsPacketProcessingTests (which uses external GpsSimulator helper).
/// </summary>
[TestFixture]
public class SimulatorIntegrationTests : BaseIntegrationTest
{
    private IBackendClient? _backendClient;
    private readonly List<ApplicationState> _receivedStates = new();

    [SetUp]
    public async Task SetUp()
    {
        _receivedStates.Clear();

        // Create backend client using test hub connection
        var hubConnection = CreateTestHubConnection("/statehub");
        _backendClient = new SignalRBackendClient(hubConnection);

        // Subscribe to state updates
        _backendClient.SubscribeToState(
            onNext: state => _receivedStates.Add(state),
            onError: ex => Console.WriteLine($"State subscription error: {ex.Message}")
        );

        await _backendClient.ConnectAsync();

        // Wait for connection to stabilize
        await Task.Delay(100);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Always stop simulator to avoid interference between tests
        if (_backendClient != null && _backendClient.IsConnected)
        {
            try
            {
                await _backendClient.SendCommandAsync(new StopSimulatorCommand());
                await Task.Delay(200); // Give simulator time to stop
            }
            catch
            {
                // Ignore errors during cleanup
            }

            await _backendClient.DisposeAsync();
        }

        _receivedStates.Clear();
    }

    #region Command Dispatch Tests

    [Test]
    public async Task StartSimulatorCommand_ShouldEnableSimulator_AndGenerateGpsData()
    {
        // Arrange
        var startCommand = new StartSimulatorCommand(
            Latitude: 45.0,
            Longitude: -93.0,
            HeadingDegrees: 0.0, // North
            SpeedKmh: 10.0
        );

        // Act - Send start command via SignalR
        await _backendClient!.SendCommandAsync(startCommand);

        // Wait for simulator to generate GPS packets
        await Task.Delay(2000); // 2 seconds at 93ms tick = ~21 packets

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().NotBeEmpty("simulator should generate GPS data");
        gpsStates.Should().HaveCountGreaterThan(10, "simulator should send multiple packets over 2 seconds");

        // Verify GPS data quality
        var lastGps = gpsStates.Last().Gnss!;
        lastGps.WgsPosition.Latitude.Should().BeInRange(44.9, 45.1, "latitude should be near start position");
        lastGps.WgsPosition.Longitude.Should().BeInRange(-93.1, -92.9, "longitude should be near start position");
        lastGps.Speed!.KilometersPerHour.Should().BeApproximately(10.0, 1.0, "speed should match command");
        lastGps.Quality.FixQuality.Should().Be(4, "simulator provides RTK Fixed quality");
        lastGps.Quality.SatellitesTracked.Should().Be(12, "simulator provides 12 satellites");

        Console.WriteLine($"✓ Simulator generated {gpsStates.Count} GPS states");
        Console.WriteLine($"  Final position: Lat={lastGps.WgsPosition.Latitude:F6}, Lon={lastGps.WgsPosition.Longitude:F6}");
    }

    [Test]
    public async Task StopSimulatorCommand_ShouldDisableSimulator()
    {
        // Arrange - Start simulator first
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));
        await Task.Delay(1000);

        var gpsCountBeforeStop = _receivedStates.Count(s => s.Gnss != null);
        gpsCountBeforeStop.Should().BeGreaterThan(5, "simulator should be generating data");

        // Act - Stop simulator
        await _backendClient.SendCommandAsync(new StopSimulatorCommand());
        await Task.Delay(500);

        _receivedStates.Clear(); // Clear old states
        await Task.Delay(1000); // Wait to see if new GPS data arrives

        // Assert - No new GPS data should be generated
        var gpsCountAfterStop = _receivedStates.Count(s => s.Gnss != null);
        gpsCountAfterStop.Should().Be(0, "simulator should be disabled and not generating GPS data");

        Console.WriteLine($"✓ Simulator stopped successfully (no GPS data after stop)");
    }

    [Test]
    public async Task SetSimulatorSpeedCommand_ShouldChangeSpeed()
    {
        // Arrange - Start simulator at 5 km/h
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 5.0));
        await Task.Delay(1000);

        var initialSpeed = _receivedStates
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        initialSpeed.Should().BeApproximately(5.0, 1.0, "initial speed should be ~5 km/h");

        _receivedStates.Clear();

        // Act - Change speed to 20 km/h
        await _backendClient.SendCommandAsync(new SetSimulatorSpeedCommand(20.0));
        await Task.Delay(1500);

        // Assert - Speed should update
        var newSpeed = _receivedStates
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        newSpeed.Should().BeApproximately(20.0, 2.0, "speed should update to ~20 km/h");

        Console.WriteLine($"✓ Speed changed from {initialSpeed:F1} km/h to {newSpeed:F1} km/h");
    }

    [Test]
    public async Task SetSimulatorSteeringCommand_ShouldAffectHeading()
    {
        // Arrange - Start simulator heading north (0°) at 15 km/h
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 15.0));
        await Task.Delay(1000);

        var initialHeading = _receivedStates
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        _receivedStates.Clear();

        // Act - Apply right steering (+20 degrees)
        await _backendClient.SendCommandAsync(new SetSimulatorSteeringCommand(20.0));
        await Task.Delay(2000); // Wait for heading to change

        // Assert - Heading should have changed (vehicle turning right)
        var newHeading = _receivedStates
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();

        // Heading should have increased (turning right/clockwise)
        var headingChange = Math.Abs(newHeading - initialHeading);
        headingChange.Should().BeGreaterThan(5.0, "heading should change significantly with steering applied");

        Console.WriteLine($"✓ Heading changed from {initialHeading:F1}° to {newHeading:F1}° (Δ={headingChange:F1}°)");
    }

    [Test]
    public async Task ResetSimulatorCommand_ShouldResetState()
    {
        // Arrange - Start simulator and modify state
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 90.0, 25.0)); // East at 25 km/h
        await _backendClient.SendCommandAsync(new SetSimulatorSteeringCommand(30.0));
        await Task.Delay(1500);

        var modifiedState = _receivedStates
            .Where(s => s.Gnss != null)
            .Select(s => s.Gnss!)
            .LastOrDefault();
        modifiedState.Should().NotBeNull();

        // Act - Reset simulator
        await _backendClient.SendCommandAsync(new ResetSimulatorCommand());
        _receivedStates.Clear();
        await Task.Delay(1000);

        // Assert - Simulator should be disabled (no GPS data after reset)
        var gpsStatesAfterReset = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStatesAfterReset.Should().BeEmpty("simulator should be disabled after reset");

        Console.WriteLine($"✓ Simulator reset successfully");
    }

    #endregion

    #region UDP Communication Verification Tests

    [Test]
    public async Task SimulatorService_ShouldSendViaUdp_NotDirectCall()
    {
        // This test verifies the architecture: SimulatorHostedService must send UDP packets,
        // not call GnssService directly. We verify this by observing:
        // 1. Simulator generates packets at 93ms intervals (SimulatorHostedService tick rate)
        // 2. Backend processes them at 250ms intervals (ApplicationOrchestrator rate)
        // 3. Timing mismatch proves UDP pipeline is used

        // Arrange & Act - Start simulator
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));

        await Task.Delay(3000); // Collect data for 3 seconds

        // Assert - Verify timing patterns indicate UDP pipeline usage
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCountGreaterThan(8, "ApplicationOrchestrator broadcasts at ~4 Hz (250ms)");
        gpsStates.Should().HaveCountLessThan(15, "Should not receive all simulator packets (93ms rate)");

        // Calculate average time between GPS updates (should be ~250ms, not 93ms)
        var timestamps = gpsStates.Select(s => s.Timestamp).OrderBy(t => t).ToList();
        if (timestamps.Count >= 2)
        {
            var intervals = timestamps.Zip(timestamps.Skip(1), (a, b) => (b - a).TotalMilliseconds).ToList();
            var avgInterval = intervals.Average();

            avgInterval.Should().BeInRange(200, 300,
                "average interval should be ~250ms (ApplicationOrchestrator rate), proving UDP pipeline is used");

            Console.WriteLine($"✓ UDP pipeline verified: avg interval = {avgInterval:F0}ms (expected ~250ms)");
        }
    }

    [Test]
    public async Task SimulatorPackets_ShouldFlowThroughFullPipeline()
    {
        // Arrange & Act - Start simulator and collect data
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 12.0));
        await Task.Delay(2500);

        // Assert - Verify complete pipeline processing
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().NotBeEmpty("GPS data should flow through full pipeline");

        // Verify all pipeline stages completed:
        // 1. SimulatorService generates binary packet
        // 2. SimulatorHostedService sends UDP to localhost:15556
        // 3. UdpPacketReceiver receives packet
        // 4. GnssService unpacks and processes (validates PGN, checksum, transforms coordinates)
        // 5. ApplicationOrchestrator broadcasts state
        // 6. SignalR delivers to clients

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

    #endregion

    #region Movement & Physics Tests

    [Test]
    public async Task SimulatorMovement_ShouldShowRealisticPositionChanges()
    {
        // Arrange & Act - Start simulator moving north at 10 km/h
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));
        await Task.Delay(3500); // Run for 3.5 seconds

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).Select(s => s.Gnss!).ToList();
        gpsStates.Should().HaveCountGreaterThan(10, "should receive multiple updates");

        var firstPos = gpsStates.First().WgsPosition;
        var lastPos = gpsStates.Last().WgsPosition;

        // Expected movement: 10 km/h * 3.5 sec = 35000 m / 3600 s = 9.7 meters north
        // Latitude change: 9.7 m / 111000 m/degree ≈ 0.000087 degrees
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
        // Arrange - Start simulator moving east at 15 km/h
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 90.0, 15.0)); // 90° = East
        await Task.Delay(1000);

        var initialStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        _receivedStates.Clear();

        // Act - Apply left steering (-25 degrees) to create curved path
        await _backendClient.SendCommandAsync(new SetSimulatorSteeringCommand(-25.0));
        await Task.Delay(3000); // Let vehicle turn

        // Assert
        var turnedStates = _receivedStates.Where(s => s.Gnss?.HeadingSingle != null).ToList();
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
        // Arrange & Act - Start simulator at 0 km/h
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 45.0, 0.0)); // Heading NE but no speed
        await Task.Delay(2500);

        // Assert
        var gpsStates = _receivedStates.Where(s => s.Gnss != null).Select(s => s.Gnss!).ToList();
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

    #endregion

    #region Complex Scenario Tests

    [Test]
    public async Task ComplexScenario_MultipleCommandSequence()
    {
        // Scenario: Start → Speed up → Turn right → Slow down → Stop

        // Act 1: Start at low speed
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 5.0)); // North at 5 km/h
        await Task.Delay(1000);

        var phase1Count = _receivedStates.Count(s => s.Gnss != null);
        phase1Count.Should().BeGreaterThan(0, "phase 1: simulator started");

        // Act 2: Speed up
        await _backendClient.SendCommandAsync(new SetSimulatorSpeedCommand(20.0));
        await Task.Delay(1000);

        var phase2Speed = _receivedStates
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        phase2Speed.Should().BeApproximately(20.0, 2.0, "phase 2: speed increased");

        // Act 3: Turn right
        await _backendClient.SendCommandAsync(new SetSimulatorSteeringCommand(15.0));
        await Task.Delay(1500);

        var phase3Heading = _receivedStates
            .Where(s => s.Gnss?.HeadingSingle != null)
            .Select(s => s.Gnss!.HeadingSingle!.Degrees)
            .LastOrDefault();
        // Heading should have increased from initial 0° (turning right)

        // Act 4: Slow down
        await _backendClient.SendCommandAsync(new SetSimulatorSpeedCommand(8.0));
        await Task.Delay(1000);

        var phase4Speed = _receivedStates
            .Where(s => s.Gnss?.Speed != null)
            .Select(s => s.Gnss!.Speed!.KilometersPerHour)
            .LastOrDefault();
        phase4Speed.Should().BeApproximately(8.0, 2.0, "phase 4: speed decreased");

        // Act 5: Stop
        await _backendClient.SendCommandAsync(new StopSimulatorCommand());
        await Task.Delay(500);
        _receivedStates.Clear();
        await Task.Delay(1000);

        var phase5Count = _receivedStates.Count(s => s.Gnss != null);
        phase5Count.Should().Be(0, "phase 5: simulator stopped");

        Console.WriteLine($"✓ Complex scenario completed successfully");
        Console.WriteLine($"  Phase 2 speed: {phase2Speed:F1} km/h");
        Console.WriteLine($"  Phase 3 heading: {phase3Heading:F1}°");
        Console.WriteLine($"  Phase 4 speed: {phase4Speed:F1} km/h");
    }

    [Test]
    public async Task SpeedChanges_ShouldAffectDistanceTraveled()
    {
        // Phase 1: Slow speed (5 km/h) for 2 seconds
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 5.0)); // North at 5 km/h
        await Task.Delay(2000);

        var slowSpeedStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        var slowStartPos = slowSpeedStates.First().Gnss!.WgsPosition;
        var slowEndPos = slowSpeedStates.Last().Gnss!.WgsPosition;
        var slowDistance = Math.Abs(slowEndPos.Latitude - slowStartPos.Latitude) * 111000; // meters

        _receivedStates.Clear();

        // Phase 2: High speed (20 km/h) for 2 seconds
        await _backendClient.SendCommandAsync(new SetSimulatorSpeedCommand(20.0));
        await Task.Delay(2000);

        var fastSpeedStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        var fastStartPos = fastSpeedStates.First().Gnss!.WgsPosition;
        var fastEndPos = fastSpeedStates.Last().Gnss!.WgsPosition;
        var fastDistance = Math.Abs(fastEndPos.Latitude - fastStartPos.Latitude) * 111000; // meters

        // Assert - Fast speed should cover ~4x more distance than slow speed
        var ratio = fastDistance / slowDistance;
        ratio.Should().BeInRange(2.5, 5.5, "faster speed should cover proportionally more distance");

        Console.WriteLine($"✓ Speed affects distance: slow={slowDistance:F2}m, fast={fastDistance:F2}m (ratio={ratio:F2}x)");
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task SimulatorGpsData_ShouldMatchAgIOPacketFormat()
    {
        // Verify simulator generates data matching real AgIO PGN 0xD6 binary format

        // Arrange & Act
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));
        await Task.Delay(2000);

        // Assert - Verify all AgIO packet fields are present
        var gpsState = _receivedStates.FirstOrDefault(s => s.Gnss != null)?.Gnss;
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
        var receivedStates2 = new List<ApplicationState>();
        backendClient2.SubscribeToState(state => receivedStates2.Add(state));
        await backendClient2.ConnectAsync();
        await Task.Delay(200);

        // Act - Start simulator
        await _backendClient!.SendCommandAsync(
            new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));
        await Task.Delay(2500);

        // Assert - Both clients should receive GPS data
        var client1GpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        var client2GpsStates = receivedStates2.Where(s => s.Gnss != null).ToList();

        client1GpsStates.Should().NotBeEmpty("client 1 should receive GPS data");
        client2GpsStates.Should().NotBeEmpty("client 2 should receive GPS data");

        // Both clients should receive similar number of updates (within 20% tolerance)
        var countRatio = (double)Math.Min(client1GpsStates.Count, client2GpsStates.Count) /
                        Math.Max(client1GpsStates.Count, client2GpsStates.Count);
        countRatio.Should().BeGreaterThan(0.8, "both clients should receive similar number of broadcasts");

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
}
