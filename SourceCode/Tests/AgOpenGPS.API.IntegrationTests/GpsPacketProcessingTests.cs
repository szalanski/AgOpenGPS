using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Client.SignalR;
using AgOpenGPS.API.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace AgOpenGPS.API.IntegrationTests;

/// <summary>
/// Integration tests for GPS packet processing end-to-end using GPS simulator.
/// Tests: GPS Simulator → UDP (hardcoded port 15556) → Backend → SignalR → Verification
/// Based on FormGPS CSim simulation logic.
/// Each test fixture gets isolated UDP port (unique per fixture).
/// </summary>
[TestFixture]
public class GpsPacketProcessingTests : BaseIntegrationTest
{
    protected override int TestFixturePort => 15556;
    private HubConnection? _hubConnection;
    private SignalRBackendClient? _subscriber;
    private ConcurrentQueue<ApplicationState>? _receivedStates;

    /// <summary>
    /// Start GPS simulator and SignalR subscriber before each test.
    /// Simulator sends PGN 0xD6 packets at 10 Hz to fixture's UDP port.
    /// </summary>
    [SetUp]
    public async Task StartSimulator()
    {
        // Setup SignalR connection for event-driven testing
        _hubConnection = CreateTestHubConnection("/statehub");
        _subscriber = new SignalRBackendClient(_hubConnection, new SignalRCommandRouter());
        _receivedStates = new ConcurrentQueue<ApplicationState>();

        await _subscriber.ConnectAsync();

        // Start the simulator with initial position, heading, and speed
        await _subscriber.SendCommandAsync(new UpdateSimulatorCommand(
            SimulatorEvent.Start(
                new Wgs84Position(45.0, -93.0),  // Start position (Minneapolis area)
                new Heading(0.0),                 // Heading north
                new Speed(10.0)                   // 10 km/h speed
            )));
    }

    /// <summary>
    /// Stop GPS simulator and SignalR subscriber after each test.
    /// </summary>
    [TearDown]
    public async Task StopSimulator()
    {
        // Stop simulator before cleanup
        if (_subscriber != null && _subscriber.IsConnected)
        {
            try
            {
                await _subscriber.SendCommandAsync(new UpdateSimulatorCommand(
                    SimulatorEvent.Stop()));
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        if (_subscriber != null)
            await _subscriber.DisposeAsync();
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }

    [Test]
    public async Task GpsSimulator_ShouldSendDataToBackend_ViaSignalR()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 15)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        _receivedStates!.Should().NotBeEmpty("should receive state updates from backend");

        var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
        gpsStates.Should().HaveCountGreaterThanOrEqualTo(15,
            "should receive multiple GPS updates from simulator (10 Hz for 2 seconds)");

        // Verify GPS data is present and valid
        var lastGps = gpsStates.Last().Gnss!;
        lastGps.WgsPosition.Latitude.Should().BeInRange(44.9, 45.1,
            "latitude should be near starting position");
        lastGps.WgsPosition.Longitude.Should().BeInRange(-93.1, -92.9,
            "longitude should be near starting position");
        lastGps.Speed!.KilometersPerHour.Should().BeApproximately(10.0, 1.0,
            "speed should match simulator setting");

        Console.WriteLine($"Received {gpsStates.Count} GPS states");
        Console.WriteLine($"Final position: Lat={lastGps.WgsPosition.Latitude:F6}, " +
                         $"Lon={lastGps.WgsPosition.Longitude:F6}");
    }

    [Test]
    public async Task GpsSimulator_ShouldUnpackBinaryDataCorrectly()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Any(s => s.Gnss != null))
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsState = _receivedStates!.FirstOrDefault(s => s.Gnss != null)?.Gnss;
        gpsState.Should().NotBeNull("GPS state should be broadcast");

        // Verify binary unpacking preserves precision
        gpsState!.WgsPosition.Latitude.Should().NotBe(0.0);
        gpsState.WgsPosition.Longitude.Should().NotBe(0.0);
        gpsState.HeadingSingle.Should().NotBeNull();
        gpsState.Speed.Should().NotBeNull();
        gpsState.Altitude.Should().NotBeNull();

        Console.WriteLine($"GPS Position: Lat={gpsState.WgsPosition.Latitude:F6}, " +
                         $"Lon={gpsState.WgsPosition.Longitude:F6}");
        Console.WriteLine($"Heading: {gpsState.HeadingSingle!.Degrees:F2}°, " +
                         $"Speed: {gpsState.Speed!.KilometersPerHour:F2} km/h");
    }

    [Test]
    public async Task GpsSimulator_ShouldTransformCoordinatesToLocalPlane()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Any(s => s.Gnss != null))
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsState = _receivedStates!.FirstOrDefault(s => s.Gnss != null)?.Gnss;
        gpsState.Should().NotBeNull();

        // Verify local plane transformation happened
        // Backend initializes local plane at (45.0, -93.0), so first position should be near origin
        var firstGps = _receivedStates.Where(s => s.Gnss != null).First().Gnss!;
        firstGps.LocalPosition.Easting.Should().BeInRange(-100.0, 100.0,
            "easting should be near zero at start (within 100m)");
        firstGps.LocalPosition.Northing.Should().BeInRange(-100.0, 100.0,
            "northing should be near zero at start (within 100m)");

        Console.WriteLine($"Local Position: E={firstGps.LocalPosition.Easting:F2}, " +
                         $"N={firstGps.LocalPosition.Northing:F2}");
    }

    [Test]
    public async Task GpsSimulator_ShouldPopulateAllFields()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Any(s => s.Gnss != null))
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsState = _receivedStates!.FirstOrDefault(s => s.Gnss != null)?.Gnss;
        gpsState.Should().NotBeNull();

        // Verify all fields from simulator are populated
        gpsState!.WgsPosition.Should().NotBeNull();
        gpsState.LocalPosition.Should().NotBeNull();
        gpsState.HeadingSingle.Should().NotBeNull();
        gpsState.HeadingSingle!.Degrees.Should().BeInRange(0.0, 360.0);
        gpsState.Speed.Should().NotBeNull();
        gpsState.Speed!.KilometersPerHour.Should().BeGreaterThan(0.0);
        gpsState.Altitude.Should().NotBeNull();
        gpsState.Altitude!.Meters.Should().BeGreaterThan(0.0);
        gpsState.Quality.SatellitesTracked.Should().Be(12, "simulator sets 12 satellites");
        gpsState.Quality.FixQuality.Should().Be(4, "simulator sets RTK Fixed quality");
        gpsState.Quality.Hdop.Should().BeApproximately(0.7, 0.1);
        gpsState.Quality.Age.Should().BeApproximately(0.1, 0.1);

        Console.WriteLine($"Satellites: {gpsState.Quality.SatellitesTracked}, " +
                         $"Fix Quality: {gpsState.Quality.FixQuality}, " +
                         $"HDOP: {gpsState.Quality.Hdop:F2}");
    }

    [Test]
    public async Task GpsSimulator_ShouldResetSentenceCounter()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Any(s => s.Gnss != null))
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsState = _receivedStates!.FirstOrDefault(s => s.Gnss != null)?.Gnss;
        gpsState.Should().NotBeNull();

        // Sentence counter should be reset to 0 on each packet
        gpsState!.Health.SentenceCounter.Should().Be(0,
            "sentence counter should be reset when GPS packet is processed");
    }

    [Test]
    public async Task GpsSimulator_ShouldShowVehicleMovement()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1);
        _subscriber!.SubscribeToState(state =>
        {
            _receivedStates!.Enqueue(state);
            if (_receivedStates.Count(s => s.Gnss != null) >= 25)
            {
                semaphore.Release();
                return;
            }
            Factory.TestTimer.AdvanceTick();
        });

        // Act
        Factory.TestTimer.AdvanceTick();  // Kick off
        await semaphore.WaitAsync();

        // Assert
        var gpsStates = _receivedStates!.Where(s => s.Gnss != null).Select(s => s.Gnss!).ToList();
        gpsStates.Should().HaveCountGreaterThanOrEqualTo(25, "should receive many GPS updates");

        // Get first and last positions
        var firstGps = gpsStates.First();
        var lastGps = gpsStates.Last();

        // Vehicle is moving north at 10 km/h for 3 seconds
        // Expected distance: 10 km/h * 3 seconds = 10000 m/3600 s * 3 s ≈ 8.3 meters
        // Latitude change: ~8.3m / 111000 m/degree ≈ 0.000075 degrees

        // Verify movement happened
        var latChange = Math.Abs(lastGps.WgsPosition.Latitude - firstGps.WgsPosition.Latitude);
        latChange.Should().BeGreaterThan(0.00005,
            "vehicle should have moved (latitude should change)");

        Console.WriteLine($"Start: Lat={firstGps.WgsPosition.Latitude:F6}, " +
                         $"Lon={firstGps.WgsPosition.Longitude:F6}");
        Console.WriteLine($"End:   Lat={lastGps.WgsPosition.Latitude:F6}, " +
                         $"Lon={lastGps.WgsPosition.Longitude:F6}");
        Console.WriteLine($"Distance moved: ~{latChange * 111000:F2} meters north");
    }
}
