using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents distance with unit conversion capabilities.
    /// Primary unit is meters (m).
    /// </summary>
    public struct Distance
    {
        /// <summary>
        /// Initializes a new instance of the Distance struct.
        /// </summary>
        /// <param name="meters">Distance in meters</param>
        public Distance(double meters)
        {
            Meters = meters;
        }

        /// <summary>
        /// Gets the distance in meters (m).
        /// </summary>
        public double Meters { get; init; }

        /// <summary>
        /// Converts the distance to kilometers (km).
        /// </summary>
        /// <returns>Distance in kilometers</returns>
        public double ToKilometers()
        {
            return Meters / 1000.0;
        }

        /// <summary>
        /// Converts the distance to miles.
        /// </summary>
        /// <returns>Distance in miles</returns>
        public double ToMiles()
        {
            return Meters * 0.000621371;
        }

        /// <summary>
        /// Creates a Distance instance from kilometers.
        /// </summary>
        /// <param name="kilometers">Distance in kilometers</param>
        /// <returns>Distance instance</returns>
        public static Distance FromKilometers(double kilometers)
        {
            return new Distance(kilometers * 1000.0);
        }

        /// <summary>
        /// Creates a Distance instance from miles.
        /// </summary>
        /// <param name="miles">Distance in miles</param>
        /// <returns>Distance instance</returns>
        public static Distance FromMiles(double miles)
        {
            return new Distance(miles / 0.000621371);
        }

        /// <summary>
        /// Returns a string representation of this distance.
        /// </summary>
        public override string ToString()
        {
            return $"{Meters:F2} m";
        }

        /// <summary>
        /// Determines whether two Distance instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Distance distance &&
                   Meters == distance.Meters;
        }

        /// <summary>
        /// Returns a hash code for this Distance.
        /// </summary>
        public override int GetHashCode()
        {
            return Meters.GetHashCode();
        }

        /// <summary>
        /// Equality operator for Distance.
        /// </summary>
        public static bool operator ==(Distance left, Distance right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for Distance.
        /// </summary>
        public static bool operator !=(Distance left, Distance right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Greater than operator for Distance.
        /// </summary>
        public static bool operator >(Distance left, Distance right)
        {
            return left.Meters > right.Meters;
        }

        /// <summary>
        /// Less than operator for Distance.
        /// </summary>
        public static bool operator <(Distance left, Distance right)
        {
            return left.Meters < right.Meters;
        }

        /// <summary>
        /// Greater than or equal operator for Distance.
        /// </summary>
        public static bool operator >=(Distance left, Distance right)
        {
            return left.Meters >= right.Meters;
        }

        /// <summary>
        /// Less than or equal operator for Distance.
        /// </summary>
        public static bool operator <=(Distance left, Distance right)
        {
            return left.Meters <= right.Meters;
        }
    }
}
