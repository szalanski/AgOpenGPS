namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Information about the local plane coordinate system.
    /// The local plane defines how WGS84 coordinates are transformed to local (easting/northing).
    /// </summary>
    public class LocalPlaneInfo
    {
        /// <summary>
        /// Gets or sets the origin point of the local plane in WGS84 coordinates.
        /// All local coordinates (easting/northing) are measured relative to this point.
        /// </summary>
        public Wgs84Position Origin { get; init; }

        /// <summary>
        /// Gets or sets the conversion factor from degrees latitude to meters at the origin.
        /// Approximately 111,320 meters per degree at all latitudes.
        /// </summary>
        public double MetersPerDegreeLat { get; init; }

        /// <summary>
        /// Gets or sets the conversion factor from degrees longitude to meters at the origin latitude.
        /// Varies with latitude: approximately 111,320 * cos(latitude) meters per degree.
        /// </summary>
        public double MetersPerDegreeLonAtOrigin { get; init; }

        /// <summary>
        /// Initializes a new instance of the LocalPlaneInfo class with default values.
        /// </summary>
        public LocalPlaneInfo()
        {
            Origin = new Wgs84Position(0, 0);
            MetersPerDegreeLat = 111320.0;
            MetersPerDegreeLonAtOrigin = 111320.0;
        }

        /// <summary>
        /// Initializes a new instance of the LocalPlaneInfo class with specified origin.
        /// </summary>
        public LocalPlaneInfo(Wgs84Position origin, double metersPerDegreeLat, double metersPerDegreeLon)
        {
            Origin = origin;
            MetersPerDegreeLat = metersPerDegreeLat;
            MetersPerDegreeLonAtOrigin = metersPerDegreeLon;
        }

        /// <summary>
        /// Returns a string representation of this local plane info.
        /// </summary>
        public override string ToString()
        {
            return $"LocalPlane origin: {Origin}, Scale: {MetersPerDegreeLat:F0}m/deg lat, {MetersPerDegreeLonAtOrigin:F0}m/deg lon";
        }
    }
}
