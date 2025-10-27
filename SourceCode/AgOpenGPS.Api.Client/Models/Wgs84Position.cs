using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents a position in the World Geodetic System 1984 (WGS84) coordinate system.
    /// This is the standard coordinate system used by GPS satellites.
    /// </summary>
    public struct Wgs84Position
    {
        /// <summary>
        /// Initializes a new instance of the Wgs84Position struct.
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees (-90 to 90, where positive is North)</param>
        /// <param name="longitude">Longitude in decimal degrees (-180 to 180, where positive is East)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when latitude or longitude are out of valid range</exception>
        public Wgs84Position(double latitude, double longitude)
        {
            if (latitude < -90.0 || latitude > 90.0)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees");

            if (longitude < -180.0 || longitude > 180.0)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees");

            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Gets the latitude in decimal degrees.
        /// Range: -90 (South Pole) to +90 (North Pole)
        /// </summary>
        public double Latitude { get; init; }

        /// <summary>
        /// Gets the longitude in decimal degrees.
        /// Range: -180 (West) to +180 (East), with 0 at the Prime Meridian
        /// </summary>
        public double Longitude { get; init; }

        /// <summary>
        /// Returns a string representation of this position in decimal degrees format.
        /// </summary>
        public override string ToString()
        {
            return $"Lat: {Latitude:F6}°, Lon: {Longitude:F6}°";
        }

        /// <summary>
        /// Determines whether two Wgs84Position instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Wgs84Position position &&
                   Latitude == position.Latitude &&
                   Longitude == position.Longitude;
        }

        /// <summary>
        /// Returns a hash code for this Wgs84Position.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Latitude.GetHashCode();
                hash = hash * 23 + Longitude.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator for Wgs84Position.
        /// </summary>
        public static bool operator ==(Wgs84Position left, Wgs84Position right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for Wgs84Position.
        /// </summary>
        public static bool operator !=(Wgs84Position left, Wgs84Position right)
        {
            return !(left == right);
        }
    }
}
