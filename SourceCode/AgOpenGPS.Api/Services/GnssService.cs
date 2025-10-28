using System.Diagnostics;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Services;

/// <summary>
/// GPS/GNSS processing service.
/// Unpacks binary GPS packets (PGN 0xD6) from AgIO, performs coordinate transformations,
/// calculates GPS state, and tracks packet reception frequency.
/// </summary>
public class GnssService : IGnssService
{
    private readonly ILogger<GnssService> _logger;
    private GnssState? _currentState;
    private CoordinateTransformer? _coordinateTransformer;

    // GPS frequency tracking
    private readonly Stopwatch _packetTimer = new();
    private bool _isFirstPacket = true;
    private double _gpsHz = 10.0; // Initial value
    private uint _sentenceCounter = 0;

    public GnssService(ILogger<GnssService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Process incoming GPS packet (PGN 0xD6).
    /// Unpacks binary data, transforms coordinates, calculates frequency, and populates GPS state.
    /// </summary>
    public void ProcessGpsPacket(byte[] packetData)
    {
        // Step 1: Calculate GPS frequency (Hz)
        var gpsHz = CalculateGpsFrequency();

        // Step 2: Extract WGS84 position
        var wgsPosition = UnpackWgs84Position(packetData);
        if (wgsPosition == null)
        {
            _logger.LogWarning("GPS packet contains invalid position (lat/lon = MaxValue)");
            return; // Invalid position - skip packet
        }

        // Step 3: Transform to local coordinates
        if (_coordinateTransformer == null)
        {
            _logger.LogWarning("Local plane not initialized - cannot transform coordinates. Call InitializeLocalPlane() first.");
            return;
        }
        var localPosition = _coordinateTransformer.ToLocal(wgsPosition.Value);

        // Step 4: Extract headings
        var headingDual = UnpackHeadingDual(packetData);
        var headingSingle = UnpackHeadingSingle(packetData);

        // Step 5: Extract motion data
        var speed = UnpackSpeed(packetData);
        var altitude = UnpackAltitude(packetData);

        // Step 6: Extract quality data
        var quality = UnpackQuality(packetData);

        // Step 7: Create GPS health
        _sentenceCounter = 0; // Reset watchdog counter
        var health = new GpsHealth(gpsHz, _sentenceCounter);

        // Step 8: Populate current state
        _currentState = new GnssState
        {
            WgsPosition = wgsPosition.Value,
            LocalPosition = localPosition,
            HeadingDual = headingDual ?? headingSingle, // Fall back to single if dual not available
            HeadingSingle = headingSingle,
            Speed = speed ?? Speed.FromMetersPerSecond(0), // Default to 0 if not available
            Altitude = altitude ?? new Altitude(0), // Default to 0 if not available
            Quality = quality,
            Health = health
        };

        _logger.LogDebug("GPS state updated: Lat={Lat:F6}, Lon={Lon:F6}, Hz={Hz:F1}, Fix={Fix}",
            wgsPosition.Value.Latitude, wgsPosition.Value.Longitude, gpsHz, quality.FixQuality);
    }

    /// <summary>
    /// Get current GPS state for broadcasting.
    /// </summary>
    public GnssState? GetCurrentState()
    {
        return _currentState;
    }

    /// <summary>
    /// Initialize local coordinate plane with field origin.
    /// Must be called before ProcessGpsPacket to enable coordinate transformations.
    /// </summary>
    public void InitializeLocalPlane(Wgs84Position origin)
    {
        _coordinateTransformer = new CoordinateTransformer(origin);
        _logger.LogInformation("Local plane initialized at {Lat:F6}, {Lon:F6}",
            origin.Latitude, origin.Longitude);
    }

    /// <summary>
    /// Extract WGS84 position from packet bytes 5-20.
    /// Returns null if position is invalid (MaxValue sentinel).
    /// </summary>
    private Wgs84Position? UnpackWgs84Position(byte[] data)
    {
        double lon = BitConverter.ToDouble(data, 5);
        double lat = BitConverter.ToDouble(data, 13);

        // Check for "no data" sentinel values
        if (lon == double.MaxValue || lat == double.MaxValue)
            return null;

        return new Wgs84Position(lat, lon);
    }

    /// <summary>
    /// Extract dual-antenna heading from packet bytes 21-24.
    /// Applies normalization to 0-360 range.
    /// Returns null if not available (MaxValue sentinel).
    /// </summary>
    private Heading? UnpackHeadingDual(byte[] data)
    {
        float headingDual = BitConverter.ToSingle(data, 21);

        if (headingDual == float.MaxValue)
            return null; // No dual antenna heading available

        // Normalize to 0-360 range
        double normalized = headingDual;
        if (normalized >= 360) normalized -= 360;
        if (normalized < 0) normalized += 360;

        return new Heading(normalized);
    }

    /// <summary>
    /// Extract single-antenna heading from packet bytes 25-28.
    /// Always present in VTG/RMC sentences.
    /// </summary>
    private Heading UnpackHeadingSingle(byte[] data)
    {
        float headingSingle = BitConverter.ToSingle(data, 25);
        return new Heading(headingSingle);
    }

    /// <summary>
    /// Extract speed from packet bytes 29-32 (km/h).
    /// Returns null if not available (MaxValue sentinel).
    /// </summary>
    private Speed? UnpackSpeed(byte[] data)
    {
        float speed = BitConverter.ToSingle(data, 29);

        if (speed == float.MaxValue)
            return null;

        return new Speed(speed); // Already in km/h
    }

    /// <summary>
    /// Extract altitude from packet bytes 37-40 (meters).
    /// Returns null if not available (MaxValue sentinel).
    /// </summary>
    private Altitude? UnpackAltitude(byte[] data)
    {
        float altitude = BitConverter.ToSingle(data, 37);

        if (altitude == float.MaxValue)
            return null;

        return new Altitude(altitude); // Already in meters
    }

    /// <summary>
    /// Extract GPS quality metrics from packet bytes 41-47.
    /// Includes satellites, fix quality, HDOP, and age.
    /// </summary>
    private GpsQuality UnpackQuality(byte[] data)
    {
        // Satellites tracked (bytes 41-42)
        ushort sats = BitConverter.ToUInt16(data, 41);
        int satellitesTracked = (sats != ushort.MaxValue) ? sats : 0;

        // Fix quality (byte 43): 0=none, 1=GPS, 2=DGPS, 4=RTK Fixed, 5=RTK Float
        byte fixQuality = data[43];
        int quality = (fixQuality != byte.MaxValue) ? fixQuality : 0;

        // HDOP (bytes 44-45) - scaled by 0.01
        ushort hdopRaw = BitConverter.ToUInt16(data, 44);
        double hdop = (hdopRaw != ushort.MaxValue) ? hdopRaw * 0.01 : 99.99;

        // Age of correction (bytes 46-47) - scaled by 0.01
        ushort ageRaw = BitConverter.ToUInt16(data, 46);
        double age = (ageRaw != ushort.MaxValue) ? ageRaw * 0.01 : 99.99;

        return new GpsQuality(quality, satellitesTracked, hdop, age);
    }

    /// <summary>
    /// Calculate GPS packet reception frequency (Hz).
    /// Uses complementary filter for smoothing.
    /// Starts timer on first packet to avoid incorrect initial measurement.
    /// </summary>
    private double CalculateGpsFrequency()
    {
        // First packet: start timer and return default frequency
        if (_isFirstPacket)
        {
            _packetTimer.Start();
            _isFirstPacket = false;
            _logger.LogDebug("GPS frequency timer started on first packet");
            return _gpsHz; // Return initial value (10 Hz)
        }

        // Measure time since last packet
        double timeSlice = _packetTimer.Elapsed.TotalSeconds;
        _packetTimer.Restart();

        // Calculate instantaneous Hz
        double nowHz = 1.0 / timeSlice;

        // Clamp to reasonable range (3-70 Hz)
        if (nowHz > 70.0) nowHz = 70.0;
        if (nowHz < 3.0) nowHz = 3.0;

        // Apply complementary filter (98% old, 2% new)
        _gpsHz = 0.98 * _gpsHz + 0.02 * nowHz;

        return _gpsHz;
    }
}
