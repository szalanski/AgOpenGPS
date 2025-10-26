using System;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Utilities;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// Transforms coordinates between WGS84 (geographic lat/lon) and local plane (easting/northing) systems.
    /// Uses WGS84 ellipsoid formulas to account for Earth's curvature.
    /// Thread-safe and immutable after construction.
    /// </summary>
    public class CoordinateTransformer
    {
        private readonly Wgs84Position _origin;
        private readonly double _metersPerDegreeLat;

        /// <summary>
        /// Initializes a new coordinate transformer with the specified origin point.
        /// </summary>
        /// <param name="origin">The origin point of the local coordinate system in WGS84 coordinates</param>
        public CoordinateTransformer(Wgs84Position origin)
        {
            _origin = origin;
            _metersPerDegreeLat = CalculateMetersPerDegreeLat(origin.Latitude);
        }

        /// <summary>
        /// Gets the origin point of this local coordinate system.
        /// </summary>
        public Wgs84Position Origin => _origin;

        /// <summary>
        /// Converts WGS84 geographic coordinates to local plane coordinates.
        /// </summary>
        /// <param name="wgs">WGS84 position (latitude/longitude)</param>
        /// <returns>Local plane position (easting/northing in meters from origin)</returns>
        public LocalPosition ToLocal(Wgs84Position wgs)
        {
            // Calculate northing: difference in latitude * meters per degree at origin latitude
            double northing = (wgs.Latitude - _origin.Latitude) * _metersPerDegreeLat;

            // Calculate easting: difference in longitude * meters per degree at current latitude
            // Note: We use the current point's latitude for more accurate conversion
            double easting = (wgs.Longitude - _origin.Longitude) * CalculateMetersPerDegreeLon(wgs.Latitude);

            return new LocalPosition(easting, northing);
        }

        /// <summary>
        /// Converts local plane coordinates to WGS84 geographic coordinates.
        /// </summary>
        /// <param name="local">Local plane position (easting/northing in meters)</param>
        /// <returns>WGS84 position (latitude/longitude)</returns>
        public Wgs84Position ToWgs84(LocalPosition local)
        {
            // Calculate latitude from northing
            double latitude = _origin.Latitude + (local.Northing / _metersPerDegreeLat);

            // Calculate longitude from easting
            // Note: We use the calculated latitude for more accurate conversion
            double longitude = _origin.Longitude + (local.Easting / CalculateMetersPerDegreeLon(latitude));

            return new Wgs84Position(latitude, longitude);
        }

        /// <summary>
        /// Calculates meters per degree of latitude at a given latitude.
        /// Formula from WGS84 ellipsoid model.
        /// Reference: https://en.wikipedia.org/wiki/Geographic_coordinate_system#Latitude_and_longitude
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <returns>Meters per degree of latitude</returns>
        private static double CalculateMetersPerDegreeLat(double latitude)
        {
            double latRad = MathHelper.DegreesToRadians(latitude);

            return 111132.92
                   - 559.82 * Math.Cos(2.0 * latRad)
                   + 1.175 * Math.Cos(4.0 * latRad)
                   - 0.0023 * Math.Cos(6.0 * latRad);
        }

        /// <summary>
        /// Calculates meters per degree of longitude at a given latitude.
        /// Longitude lines converge at the poles, so this varies with latitude.
        /// Formula from WGS84 ellipsoid model.
        /// Reference: https://en.wikipedia.org/wiki/Geographic_coordinate_system#Latitude_and_longitude
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <returns>Meters per degree of longitude at the given latitude</returns>
        private static double CalculateMetersPerDegreeLon(double latitude)
        {
            double latRad = MathHelper.DegreesToRadians(latitude);

            return 111412.84 * Math.Cos(latRad)
                   - 93.5 * Math.Cos(3.0 * latRad)
                   + 0.118 * Math.Cos(5.0 * latRad);
        }
    }
}
