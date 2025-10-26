using System.Net.Sockets;

namespace AgOpenGPS.API.IntegrationTests.Helpers;

/// <summary>
/// Simulates GPS data similar to FormGPS simulator (CSim.cs).
/// Generates vehicle movement and sends PGN 0xD6 packets to backend via UDP.
/// Runs as background task during integration tests.
/// </summary>
public class GpsSimulator : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly CancellationTokenSource _cts;
    private Task? _simulationTask;

    // Simulation state
    private double _latitude;
    private double _longitude;
    private double _heading; // radians
    private double _speed; // km/h
    private double _altitude;
    private readonly int _updateIntervalMs;

    /// <summary>
    /// Creates a GPS simulator with configurable parameters.
    /// </summary>
    /// <param name="startLatitude">Starting latitude in degrees</param>
    /// <param name="startLongitude">Starting longitude in degrees</param>
    /// <param name="headingDegrees">Heading in degrees (0 = North, 90 = East)</param>
    /// <param name="speedKmh">Speed in kilometers per hour</param>
    /// <param name="updateIntervalMs">Update interval in milliseconds (default 100ms = 10 Hz)</param>
    public GpsSimulator(
        double startLatitude = 45.0,
        double startLongitude = -93.0,
        double headingDegrees = 0.0,
        double speedKmh = 10.0,
        int updateIntervalMs = 100)
    {
        _latitude = startLatitude;
        _longitude = startLongitude;
        _heading = headingDegrees * Math.PI / 180.0; // Convert to radians
        _speed = speedKmh;
        _altitude = SimulateAltitude(_latitude, _longitude);
        _updateIntervalMs = updateIntervalMs;

        _udpClient = new UdpClient();
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Start simulating GPS updates in background.
    /// </summary>
    public void Start()
    {
        if (_simulationTask != null)
        {
            throw new InvalidOperationException("Simulator already started");
        }

        _simulationTask = Task.Run(SimulationLoop);
        Console.WriteLine($"GPS Simulator started: Lat={_latitude:F6}, Lon={_longitude:F6}, " +
                         $"Heading={_heading * 180.0 / Math.PI:F1}°, Speed={_speed:F1} km/h");
    }

    /// <summary>
    /// Stop simulation.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        _simulationTask?.Wait(TimeSpan.FromSeconds(2));
        Console.WriteLine("GPS Simulator stopped");
    }

    private async Task SimulationLoop()
    {
        int packetCount = 0;

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Calculate movement distance for this timestep
                // Distance = speed * time (converted to kilometers)
                double stepDistanceKm = _speed / 3600.0 * (_updateIntervalMs / 1000.0);

                // Update position based on heading and distance
                // Similar to CSim.DoSimTick() → CurrentLatLon.CalculateNewPostionFromBearingDistance
                (_latitude, _longitude) = CalculateNewPosition(
                    _latitude, _longitude, _heading, stepDistanceKm);

                // Update altitude based on position (mimics FormGPS SimulateAltitude)
                _altitude = SimulateAltitude(_latitude, _longitude);

                // Generate PGN 0xD6 GPS packet (same format AgIO sends)
                var packet = AgIOPacketSimulator.CreateValidGpsPacket(
                    latitude: _latitude,
                    longitude: _longitude,
                    headingSingle: (float)(_heading * 180.0 / Math.PI), // Convert to degrees
                    speed: (float)_speed,
                    altitude: (float)_altitude,
                    satellites: 12,
                    fixQuality: 4, // RTK Fixed
                    hdop: 0.7,
                    age: 0.1
                );

                // Send to backend port 15556 (backend uses 15556, FormGPS uses 15555)
                await _udpClient.SendAsync(packet, packet.Length, "127.0.0.1", 15556);

                packetCount++;
                if (packetCount % 50 == 0) // Log every 5 seconds at 10 Hz
                {
                    Console.WriteLine($"GPS Simulator: {packetCount} packets sent, " +
                                     $"Position: Lat={_latitude:F6}, Lon={_longitude:F6}");
                }

                await Task.Delay(_updateIntervalMs, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPS Simulator error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Calculate new lat/lon position given bearing and distance.
    /// Simplified WGS84 calculation using great circle navigation.
    /// Based on FormGPS: Wgs84.CalculateNewPostionFromBearingDistance
    /// </summary>
    private (double lat, double lon) CalculateNewPosition(
        double lat, double lon, double bearing, double distanceKm)
    {
        const double earthRadiusKm = 6371.0;

        // Convert lat/lon to radians
        double latRad = lat * Math.PI / 180.0;
        double lonRad = lon * Math.PI / 180.0;

        // Calculate angular distance
        double angularDistance = distanceKm / earthRadiusKm;

        // Calculate new latitude using great circle formula
        double newLatRad = Math.Asin(
            Math.Sin(latRad) * Math.Cos(angularDistance) +
            Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearing));

        // Calculate new longitude using great circle formula
        double newLonRad = lonRad + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(latRad),
            Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

        // Convert back to degrees
        return (newLatRad * 180.0 / Math.PI, newLonRad * 180.0 / Math.PI);
    }

    /// <summary>
    /// Simulate altitude based on lat/lon (mimics FormGPS CSim.SimulateAltitude).
    /// Creates varying altitude based on geographic position.
    /// </summary>
    private double SimulateAltitude(double lat, double lon)
    {
        double temp = Math.Abs(lat * 100);
        temp -= ((int)(temp));
        temp *= 100;
        double altitude = temp + 200;

        temp = Math.Abs(lon * 100);
        temp -= ((int)(temp));
        temp *= 100;
        altitude += temp;

        return altitude;
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        _udpClient.Dispose();
    }
}
