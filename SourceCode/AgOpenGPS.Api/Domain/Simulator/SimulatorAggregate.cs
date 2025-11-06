using System;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Domain.Simulator
{
    /// <summary>
    /// Aggregate root that encapsulates simulator state and enforces invariants.
    /// All domain state transitions happen through this type.
    /// </summary>
    public sealed class SimulatorAggregate
    {
        private const double MinSpeedKph = 0.0;
        private const double MaxSpeedKph = 322.0;
        private const double AccelerationRatePerTick = 0.86;
        private const double DecelerationRatePerTick = 0.43;

        public SimulatorAggregate()
        {
            CurrentPosition = new Wgs84Position(45.0, -93.0);
            InitialPosition = CurrentPosition;
            CurrentHeading = new Heading(0.0);
            CurrentSpeed = new Speed(0.0);
            TargetSpeed = CurrentSpeed;
            TargetSteering = SteeringAngle.Zero;
            SmoothedSteering = SteeringAngle.Zero;
            IsEnabled = false;
        }

        public bool IsEnabled { get; private set; }
        public Wgs84Position CurrentPosition { get; private set; }
        public Wgs84Position InitialPosition { get; private set; }
        public Heading CurrentHeading { get; private set; }
        public Speed CurrentSpeed { get; private set; }
        public Speed TargetSpeed { get; private set; }
        public SteeringAngle TargetSteering { get; private set; }
        public SteeringAngle SmoothedSteering { get; private set; }

        /// <summary>
        /// Starts the simulator at the supplied position, heading, and speed.
        /// </summary>
        public void Start(Wgs84Position position, Heading heading, Speed speed)
        {
            CurrentPosition = position;
            InitialPosition = position;
            CurrentHeading = heading;
            var clamped = ClampSpeed(speed.KilometersPerHour);
            CurrentSpeed = clamped;
            TargetSpeed = clamped;
            TargetSteering = SteeringAngle.Zero;
            SmoothedSteering = SteeringAngle.Zero;
            IsEnabled = true;
        }

        /// <summary>
        /// Stops the simulator. State is preserved so it can be resumed.
        /// </summary>
        public void Stop() => IsEnabled = false;

        /// <summary>
        /// Sets current and target speed instantly (used for hard changes and resets).
        /// </summary>
        public void SetInstantSpeed(Speed speed)
        {
            var clamped = ClampSpeed(speed.KilometersPerHour);
            CurrentSpeed = clamped;
            TargetSpeed = clamped;
        }

        /// <summary>
        /// Sets target speed for smooth transition.
        /// </summary>
        public void SetTargetSpeed(Speed speed)
        {
            TargetSpeed = ClampSpeed(speed.KilometersPerHour);
        }

        /// <summary>
        /// Adjusts the target speed by delta in km/h, respecting bounds.
        /// </summary>
        public void AdjustTargetSpeed(double deltaKph)
        {
            var updated = Math.Clamp(TargetSpeed.KilometersPerHour + deltaKph, MinSpeedKph, MaxSpeedKph);
            TargetSpeed = new Speed(updated);
        }

        /// <summary>
        /// Resets current and target speeds to zero.
        /// </summary>
        public void ZeroSpeed()
        {
            var zero = new Speed(0.0);
            CurrentSpeed = zero;
            TargetSpeed = zero;
        }

        /// <summary>
        /// Sets target steering angle used for smoothing.
        /// </summary>
        public void SetTargetSteering(SteeringAngle steeringAngle)
        {
            TargetSteering = steeringAngle;
        }

        /// <summary>
        /// Resets steering angles back to straight.
        /// </summary>
        public void ResetSteering()
        {
            TargetSteering = SteeringAngle.Zero;
            SmoothedSteering = SteeringAngle.Zero;
        }

        /// <summary>
        /// Reverses heading by 180 degrees.
        /// </summary>
        public void ReverseDirection()
        {
            CurrentHeading = CurrentHeading + 180.0;
        }

        /// <summary>
        /// Resets current position to supplied coordinates.
        /// </summary>
        public void ResetPosition(Wgs84Position position)
        {
            CurrentPosition = position;
        }

        /// <summary>
        /// Resets position to the initially recorded start position.
        /// </summary>
        public void ResetToInitialPosition()
        {
            CurrentPosition = InitialPosition;
        }

        /// <summary>
        /// Advances the simulation state by one tick.
        /// </summary>
        public void AdvanceTick(TimeSpan tickInterval, VehiclePhysicsDomainService physics)
        {
            CurrentSpeed = physics.TransitionSpeed(CurrentSpeed, TargetSpeed, AccelerationRatePerTick, DecelerationRatePerTick);
            SmoothedSteering = physics.SmoothSteeringAngle(SmoothedSteering, TargetSteering);

            var stepDistance = physics.CalculateStepDistance(CurrentSpeed, tickInterval);
            CurrentHeading = physics.UpdateHeading(CurrentHeading, SmoothedSteering, stepDistance);
            CurrentPosition = physics.CalculateNewPosition(CurrentPosition, CurrentHeading, stepDistance);
        }

        private static Speed ClampSpeed(double kilometersPerHour)
        {
            var clamped = Math.Clamp(kilometersPerHour, MinSpeedKph, MaxSpeedKph);
            return new Speed(clamped);
        }
    }
}
