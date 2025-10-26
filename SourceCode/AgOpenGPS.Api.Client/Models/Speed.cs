using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents ground speed with unit conversion capabilities.
    /// Primary unit is kilometers per hour (km/h).
    /// </summary>
    public struct Speed
    {
        /// <summary>
        /// Initializes a new instance of the Speed struct.
        /// </summary>
        /// <param name="kilometersPerHour">Speed in kilometers per hour</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is negative</exception>
        public Speed(double kilometersPerHour)
        {
            if (kilometersPerHour < 0)
                throw new ArgumentOutOfRangeException(nameof(kilometersPerHour), "Speed cannot be negative");

            KilometersPerHour = kilometersPerHour;
        }

        /// <summary>
        /// Gets the speed in kilometers per hour (km/h).
        /// </summary>
        public double KilometersPerHour { get; init; }

        /// <summary>
        /// Converts the speed to meters per second (m/s).
        /// </summary>
        /// <returns>Speed in meters per second</returns>
        public double ToMetersPerSecond()
        {
            return KilometersPerHour / 3.6;
        }

        /// <summary>
        /// Converts the speed to miles per hour (mph).
        /// </summary>
        /// <returns>Speed in miles per hour</returns>
        public double ToMilesPerHour()
        {
            return KilometersPerHour * 0.621371;
        }

        /// <summary>
        /// Creates a Speed instance from meters per second.
        /// </summary>
        /// <param name="metersPerSecond">Speed in meters per second</param>
        /// <returns>Speed instance</returns>
        public static Speed FromMetersPerSecond(double metersPerSecond)
        {
            return new Speed(metersPerSecond * 3.6);
        }

        /// <summary>
        /// Creates a Speed instance from miles per hour.
        /// </summary>
        /// <param name="milesPerHour">Speed in miles per hour</param>
        /// <returns>Speed instance</returns>
        public static Speed FromMilesPerHour(double milesPerHour)
        {
            return new Speed(milesPerHour / 0.621371);
        }

        /// <summary>
        /// Returns a string representation of this speed.
        /// </summary>
        public override string ToString()
        {
            return $"{KilometersPerHour:F1} km/h";
        }

        /// <summary>
        /// Determines whether two Speed instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Speed speed &&
                   KilometersPerHour == speed.KilometersPerHour;
        }

        /// <summary>
        /// Returns a hash code for this Speed.
        /// </summary>
        public override int GetHashCode()
        {
            return KilometersPerHour.GetHashCode();
        }

        /// <summary>
        /// Equality operator for Speed.
        /// </summary>
        public static bool operator ==(Speed left, Speed right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for Speed.
        /// </summary>
        public static bool operator !=(Speed left, Speed right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Greater than operator for Speed.
        /// </summary>
        public static bool operator >(Speed left, Speed right)
        {
            return left.KilometersPerHour > right.KilometersPerHour;
        }

        /// <summary>
        /// Less than operator for Speed.
        /// </summary>
        public static bool operator <(Speed left, Speed right)
        {
            return left.KilometersPerHour < right.KilometersPerHour;
        }

        /// <summary>
        /// Greater than or equal operator for Speed.
        /// </summary>
        public static bool operator >=(Speed left, Speed right)
        {
            return left.KilometersPerHour >= right.KilometersPerHour;
        }

        /// <summary>
        /// Less than or equal operator for Speed.
        /// </summary>
        public static bool operator <=(Speed left, Speed right)
        {
            return left.KilometersPerHour <= right.KilometersPerHour;
        }
    }
}
