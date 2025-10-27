using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents a compass heading (true north) in degrees.
    /// Valid range is 0-360 degrees, where 0/360 is North, 90 is East, 180 is South, and 270 is West.
    /// </summary>
    public struct Heading
    {
        private double _degrees;

        /// <summary>
        /// Initializes a new instance of the Heading struct.
        /// The value is automatically normalized to the range [0, 360).
        /// </summary>
        /// <param name="degrees">Heading in degrees (will be normalized to 0-360)</param>
        public Heading(double degrees)
        {
            _degrees = Normalize(degrees);
        }

        /// <summary>
        /// Gets the heading in degrees, normalized to the range [0, 360).
        /// 0° = North, 90° = East, 180° = South, 270° = West
        /// </summary>
        public double Degrees
        {
            get => _degrees;
            init => _degrees = Normalize(value);
        }

        /// <summary>
        /// Converts the heading to radians.
        /// </summary>
        /// <returns>Heading in radians (0 to 2π)</returns>
        public double ToRadians()
        {
            return _degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Calculates the smallest angular difference between this heading and another.
        /// Result is positive if the target is clockwise, negative if counterclockwise.
        /// </summary>
        /// <param name="target">The target heading</param>
        /// <returns>Angular difference in degrees (-180 to +180)</returns>
        public double DifferenceTo(Heading target)
        {
            double diff = target._degrees - _degrees;

            // Normalize to [-180, 180]
            while (diff > 180.0) diff -= 360.0;
            while (diff < -180.0) diff += 360.0;

            return diff;
        }

        /// <summary>
        /// Normalizes an angle to the range [0, 360).
        /// </summary>
        private static double Normalize(double degrees)
        {
            degrees = degrees % 360.0;
            if (degrees < 0)
                degrees += 360.0;
            return degrees;
        }

        /// <summary>
        /// Returns a string representation of this heading.
        /// </summary>
        public override string ToString()
        {
            return $"{_degrees:F1}°";
        }

        /// <summary>
        /// Determines whether two Heading instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Heading heading &&
                   _degrees == heading._degrees;
        }

        /// <summary>
        /// Returns a hash code for this Heading.
        /// </summary>
        public override int GetHashCode()
        {
            return _degrees.GetHashCode();
        }

        /// <summary>
        /// Equality operator for Heading.
        /// </summary>
        public static bool operator ==(Heading left, Heading right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for Heading.
        /// </summary>
        public static bool operator !=(Heading left, Heading right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Addition operator - adds degrees to the heading and normalizes.
        /// </summary>
        public static Heading operator +(Heading heading, double degrees)
        {
            return new Heading(heading._degrees + degrees);
        }

        /// <summary>
        /// Subtraction operator - subtracts degrees from the heading and normalizes.
        /// </summary>
        public static Heading operator -(Heading heading, double degrees)
        {
            return new Heading(heading._degrees - degrees);
        }
    }
}
