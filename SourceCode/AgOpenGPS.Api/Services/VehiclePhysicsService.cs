using System;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// Vehicle physics calculations for simulator.
    /// Handles speed transitions, steering smoothing, heading changes, and position calculations.
    /// </summary>
    public class VehiclePhysicsService
    {
        private const double DEG_TO_RAD = Math.PI / 180.0;
        private const double RAD_TO_DEG = 180.0 / Math.PI;
        private const double EARTH_RADIUS_KM = 6371.0;
        private const double TWO_PI = 2.0 * Math.PI;

        /// <summary>
        /// Apply smooth speed transition with acceleration/deceleration rates.
        /// </summary>
        public double TransitionSpeed(double currentSpeed, double targetSpeed, double accelerationRate, double decelerationRate)
        {
            if (Math.Abs(currentSpeed - targetSpeed) <= 0.01)
                return targetSpeed;

            double rate = (currentSpeed < targetSpeed) ? accelerationRate : decelerationRate;
            double speedDiff = targetSpeed - currentSpeed;

            if (Math.Abs(speedDiff) < rate)
                return targetSpeed;

            return currentSpeed + Math.Sign(speedDiff) * rate;
        }

        /// <summary>
        /// Smooth steering angle transition (from CSim.DoSimTick).
        /// </summary>
        public double SmoothSteeringAngle(double currentAngleAve, double targetAngle)
        {
            double diff = Math.Abs(targetAngle - currentAngleAve);

            if (diff > 11)
                return currentAngleAve + (targetAngle > currentAngleAve ? 6 : -6);

            if (diff > 5)
                return currentAngleAve + (targetAngle > currentAngleAve ? 2 : -2);

            if (diff > 1)
                return currentAngleAve + (targetAngle > currentAngleAve ? 0.5 : -0.5);

            return targetAngle;
        }

        /// <summary>
        /// Calculate heading change based on steering angle and step distance.
        /// IMPORTANT: stepDistance must be in METRES (legacy CSim formula).
        /// </summary>
        /// <param name="steerAngleAve">Steering angle in degrees</param>
        /// <param name="stepDistance">Step distance in METRES (not kilometres)</param>
        /// <returns>Heading change in radians</returns>
        public double CalculateHeadingChange(double steerAngleAve, double stepDistance)
        {
            return stepDistance * Math.Tan(steerAngleAve * DEG_TO_RAD) / 2.0;
        }

        /// <summary>
        /// Normalize heading to [0, 2π) range.
        /// </summary>
        public double NormalizeHeading(double headingRad)
        {
            while (headingRad >= TWO_PI)
                headingRad -= TWO_PI;

            while (headingRad < 0)
                headingRad += TWO_PI;

            return headingRad;
        }

        /// <summary>
        /// Great circle navigation - calculates new position from bearing and distance.
        /// </summary>
        public (double lat, double lon) CalculateNewPosition(double latDeg, double lonDeg, double bearingRad, double distanceKm)
        {
            double latRad = latDeg * DEG_TO_RAD;
            double lonRad = lonDeg * DEG_TO_RAD;
            double angularDistance = distanceKm / EARTH_RADIUS_KM;

            double newLatRad = Math.Asin(
                Math.Sin(latRad) * Math.Cos(angularDistance) +
                Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return (newLatRad * RAD_TO_DEG, newLonRad * RAD_TO_DEG);
        }
    }
}
