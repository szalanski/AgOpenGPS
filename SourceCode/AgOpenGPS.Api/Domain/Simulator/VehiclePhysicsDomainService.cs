using System;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Domain.Simulator
{
    /// <summary>
    /// Domain service responsible for vehicle physics calculations used by the simulator aggregate.
    /// </summary>
    public class VehiclePhysicsDomainService
    {
        private const double DegToRad = Math.PI / 180.0;
        private const double RadToDeg = 180.0 / Math.PI;
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Applies smooth speed transition with acceleration/deceleration rates.
        /// </summary>
        public Speed TransitionSpeed(Speed currentSpeed, Speed targetSpeed, double accelerationRate, double decelerationRate)
        {
            double current = currentSpeed.KilometersPerHour;
            double target = targetSpeed.KilometersPerHour;

            if (Math.Abs(current - target) <= 0.01)
                return targetSpeed;

            double rate = current < target ? accelerationRate : decelerationRate;
            double speedDiff = target - current;

            if (Math.Abs(speedDiff) < rate)
                return targetSpeed;

            return new Speed(current + Math.Sign(speedDiff) * rate);
        }

        /// <summary>
        /// Smooth steering angle transition (legacy behaviour retained for realism).
        /// </summary>
        public SteeringAngle SmoothSteeringAngle(SteeringAngle currentAngle, SteeringAngle targetAngle)
        {
            double current = currentAngle.Degrees;
            double target = targetAngle.Degrees;
            double diff = Math.Abs(target - current);

            if (diff > 11)
                return new SteeringAngle(current + (target > current ? 6 : -6));

            if (diff > 5)
                return new SteeringAngle(current + (target > current ? 2 : -2));

            if (diff > 1)
                return new SteeringAngle(current + (target > current ? 0.5 : -0.5));

            return targetAngle;
        }

        /// <summary>
        /// Calculates travelled distance during the tick using the supplied interval.
        /// </summary>
        public Distance CalculateStepDistance(Speed speed, TimeSpan tickInterval)
        {
            double meters = speed.ToMetersPerSecond() * tickInterval.TotalSeconds;
            return new Distance(meters);
        }

        /// <summary>
        /// Updates heading based on steering angle and travelled distance.
        /// </summary>
        public Heading UpdateHeading(Heading heading, SteeringAngle steeringAngle, Distance stepDistance)
        {
            double headingChangeRadians = stepDistance.Meters * Math.Tan(steeringAngle.Radians) / 2.0;
            double headingChangeDegrees = headingChangeRadians * RadToDeg;
            return heading + headingChangeDegrees;
        }

        /// <summary>
        /// Great circle navigation - calculates new position from bearing and distance.
        /// </summary>
        public Wgs84Position CalculateNewPosition(Wgs84Position currentPosition, Heading bearing, Distance distance)
        {
            double latRad = currentPosition.Latitude * DegToRad;
            double lonRad = currentPosition.Longitude * DegToRad;
            double bearingRad = bearing.ToRadians();
            double angularDistance = distance.ToKilometers() / EarthRadiusKm;

            double newLatRad = Math.Asin(
                Math.Sin(latRad) * Math.Cos(angularDistance) +
                Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            double newLonRad = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return new Wgs84Position(newLatRad * RadToDeg, newLonRad * RadToDeg);
        }
    }
}
