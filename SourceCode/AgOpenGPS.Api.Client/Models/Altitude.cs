using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents altitude/elevation with unit conversion capabilities.
    /// Primary unit is meters above mean sea level (MSL).
    /// </summary>
    public struct Altitude
    {
        /// <summary>
        /// Initializes a new instance of the Altitude struct.
        /// </summary>
        /// <param name="meters">Altitude in meters above mean sea level (MSL)</param>
        public Altitude(double meters)
        {
            Meters = meters;
        }

        /// <summary>
        /// Gets the altitude in meters above mean sea level (MSL).
        /// </summary>
        public double Meters { get; init; }

        /// <summary>
        /// Converts the altitude to feet.
        /// </summary>
        /// <returns>Altitude in feet</returns>
        public double ToFeet()
        {
            return Meters * 3.28084;
        }

        /// <summary>
        /// Creates an Altitude instance from feet.
        /// </summary>
        /// <param name="feet">Altitude in feet</param>
        /// <returns>Altitude instance</returns>
        public static Altitude FromFeet(double feet)
        {
            return new Altitude(feet / 3.28084);
        }

        /// <summary>
        /// Returns a string representation of this altitude.
        /// </summary>
        public override string ToString()
        {
            return $"{Meters:F1} m";
        }

        /// <summary>
        /// Determines whether two Altitude instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Altitude altitude &&
                   Meters == altitude.Meters;
        }

        /// <summary>
        /// Returns a hash code for this Altitude.
        /// </summary>
        public override int GetHashCode()
        {
            return Meters.GetHashCode();
        }

        /// <summary>
        /// Equality operator for Altitude.
        /// </summary>
        public static bool operator ==(Altitude left, Altitude right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for Altitude.
        /// </summary>
        public static bool operator !=(Altitude left, Altitude right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Greater than operator for Altitude.
        /// </summary>
        public static bool operator >(Altitude left, Altitude right)
        {
            return left.Meters > right.Meters;
        }

        /// <summary>
        /// Less than operator for Altitude.
        /// </summary>
        public static bool operator <(Altitude left, Altitude right)
        {
            return left.Meters < right.Meters;
        }

        /// <summary>
        /// Greater than or equal operator for Altitude.
        /// </summary>
        public static bool operator >=(Altitude left, Altitude right)
        {
            return left.Meters >= right.Meters;
        }

        /// <summary>
        /// Less than or equal operator for Altitude.
        /// </summary>
        public static bool operator <=(Altitude left, Altitude right)
        {
            return left.Meters <= right.Meters;
        }

        /// <summary>
        /// Addition operator - adds meters to the altitude.
        /// </summary>
        public static Altitude operator +(Altitude altitude, double meters)
        {
            return new Altitude(altitude.Meters + meters);
        }

        /// <summary>
        /// Subtraction operator - subtracts meters from the altitude.
        /// </summary>
        public static Altitude operator -(Altitude altitude, double meters)
        {
            return new Altitude(altitude.Meters - meters);
        }

        /// <summary>
        /// Subtraction operator - calculates difference between two altitudes.
        /// </summary>
        public static double operator -(Altitude left, Altitude right)
        {
            return left.Meters - right.Meters;
        }
    }
}
