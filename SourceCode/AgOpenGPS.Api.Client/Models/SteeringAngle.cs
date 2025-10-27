using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents a steering angle for vehicle simulation.
    /// Positive values indicate right turn, negative values indicate left turn, zero is straight ahead.
    /// </summary>
    public record SteeringAngle
    {
        /// <summary>
        /// Zero steering angle (straight ahead).
        /// </summary>
        public static readonly SteeringAngle Zero = new SteeringAngle(0.0);

        /// <summary>
        /// Initializes a new instance of the SteeringAngle record.
        /// </summary>
        /// <param name="Degrees">Steering angle in degrees (positive = right, negative = left)</param>
        public SteeringAngle(double Degrees)
        {
            this.Degrees = Degrees;
        }

        /// <summary>
        /// Gets the steering angle in degrees.
        /// Positive = right turn, Negative = left turn, Zero = straight
        /// </summary>
        public double Degrees { get; init; }

        /// <summary>
        /// Converts the steering angle to radians.
        /// </summary>
        /// <returns>Steering angle in radians</returns>
        public double ToRadians()
        {
            return Degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Calculates the absolute difference between this steering angle and another.
        /// </summary>
        /// <param name="other">The other steering angle</param>
        /// <returns>Absolute difference in degrees</returns>
        public double AbsoluteDifferenceTo(SteeringAngle other)
        {
            return Math.Abs(Degrees - other.Degrees);
        }

        /// <summary>
        /// Returns a string representation of this steering angle.
        /// </summary>
        public override string ToString()
        {
            string direction = Degrees > 0 ? "R" : Degrees < 0 ? "L" : "straight";
            return $"{Math.Abs(Degrees).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}° {direction}";
        }
    }
}
