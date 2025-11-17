using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Abstractions;

/// <summary>
/// Service responsible for local plane coordinate transformations.
/// Manages the local plane origin and provides conversion between WGS84 geographic
/// coordinates and local plane coordinates (easting/northing).
/// </summary>
public interface ICoordinateService
{
    /// <summary>
    /// Initialize local plane with field origin.
    /// Must be called before coordinate conversions.
    /// </summary>
    /// <param name="origin">WGS84 position to use as local plane origin</param>
    void InitializeLocalPlane(Wgs84Position origin);

    /// <summary>
    /// Check if local plane has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Get current local plane origin (null if not initialized).
    /// </summary>
    Wgs84Position? Origin { get; }

    /// <summary>
    /// Convert WGS84 geographic coordinates to local plane coordinates.
    /// </summary>
    /// <param name="wgs84">WGS84 position to convert</param>
    /// <returns>Local plane position (easting/northing in meters)</returns>
    /// <exception cref="InvalidOperationException">Thrown if local plane not initialized</exception>
    LocalPosition ConvertToLocal(Wgs84Position wgs84);

    /// <summary>
    /// Convert local plane coordinates to WGS84 geographic coordinates.
    /// </summary>
    /// <param name="local">Local plane position to convert</param>
    /// <returns>WGS84 position</returns>
    /// <exception cref="InvalidOperationException">Thrown if local plane not initialized</exception>
    Wgs84Position ConvertToWgs84(LocalPosition local);

    /// <summary>
    /// Get local plane information for diagnostics/UI.
    /// Returns null if not initialized.
    /// </summary>
    LocalPlaneInfo? GetLocalPlaneInfo();

    /// <summary>
    /// Update local plane origin (e.g., when loading different field).
    /// </summary>
    /// <param name="newOrigin">New WGS84 position to use as origin</param>
    void UpdateOrigin(Wgs84Position newOrigin);

    /// <summary>
    /// Set or update the local plane origin.
    /// Works whether the coordinate system is already initialized or not.
    /// </summary>
    /// <param name="origin">WGS84 position to use as local plane origin</param>
    void SetOrigin(Wgs84Position origin);
}
