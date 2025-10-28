using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents a position in a local plane coordinate system (flat-earth approximation).
    /// Uses eastings and northings in meters relative to a local origin point.
    /// This coordinate system is used for field-level operations where the earth's curvature can be ignored.
    /// </summary>
    public struct LocalPosition
    {
        /// <summary>
        /// Initializes a new instance of the LocalPosition struct.
        /// </summary>
        /// <param name="easting">East-west position in meters (positive is East from origin)</param>
        /// <param name="northing">North-south position in meters (positive is North from origin)</param>
        public LocalPosition(double easting, double northing)
        {
            Easting = easting;
            Northing = northing;
        }

        /// <summary>
        /// Gets the easting coordinate in meters.
        /// Positive values are to the East of the local origin.
        /// </summary>
        public double Easting { get; init; }

        /// <summary>
        /// Gets the northing coordinate in meters.
        /// Positive values are to the North of the local origin.
        /// </summary>
        public double Northing { get; init; }

        /// <summary>
        /// Calculates the Euclidean distance to another local position.
        /// </summary>
        /// <param name="other">The other position</param>
        /// <returns>Distance in meters</returns>
        public double DistanceTo(LocalPosition other)
        {
            double de = Easting - other.Easting;
            double dn = Northing - other.Northing;
            return Math.Sqrt(de * de + dn * dn);
        }

        /// <summary>
        /// Returns a string representation of this position.
        /// </summary>
        public override string ToString()
        {
            return $"E: {Easting:F2}m, N: {Northing:F2}m";
        }

        /// <summary>
        /// Determines whether two LocalPosition instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is LocalPosition position &&
                   Easting == position.Easting &&
                   Northing == position.Northing;
        }

        /// <summary>
        /// Returns a hash code for this LocalPosition.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Easting.GetHashCode();
                hash = hash * 23 + Northing.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator for LocalPosition.
        /// </summary>
        public static bool operator ==(LocalPosition left, LocalPosition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for LocalPosition.
        /// </summary>
        public static bool operator !=(LocalPosition left, LocalPosition right)
        {
            return !(left == right);
        }
    }
}
