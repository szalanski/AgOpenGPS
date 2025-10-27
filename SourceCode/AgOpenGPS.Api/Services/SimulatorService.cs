using System;
using AgOpenGPS.Api.Client.Models;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// GPS simulator service. Generates simulated GPS data for testing.
    /// Based on FormGPS CSim.DoSimTick() logic.
    /// </summary>
    public class SimulatorService
    {
        private const double RAD_TO_DEG = 180.0 / Math.PI;

        // Thread synchronization
        private readonly object _lock = new object();

        // Injected services
        private readonly VehiclePhysicsService _physics;
        private readonly GnssDataGenerator _gnssGenerator;
        private readonly AgIoProtocolSerializer _serializer;

        // Simulator state (value objects)
        private Wgs84Position _currentPosition;
        private Wgs84Position _initialPosition;  // For Reset() - legacy behavior
        private Heading _currentHeading;
        private Speed _currentSpeed;
        private Speed _targetSpeed;             // For smooth speed transitions
        private SteeringAngle _targetSteering;
        private SteeringAngle _smoothedSteering;
        private double _stepDistance;           // Calculated per tick (intermediate value)

        // Smooth speed transition rates
        private const double ACCELERATION_RATE = 0.86;  // km/h per tick (matches legacy)
        private const double DECELERATION_RATE = 0.43;  // km/h per tick (matches legacy)

        public bool IsEnabled { get; private set; }

        public SimulatorService(
            VehiclePhysicsService physics,
            GnssDataGenerator gnssGenerator,
            AgIoProtocolSerializer serializer)
        {
            _physics = physics;
            _gnssGenerator = gnssGenerator;
            _serializer = serializer;

            IsEnabled = false;
            _currentPosition = new Wgs84Position(45.0, -93.0);
            _initialPosition = new Wgs84Position(45.0, -93.0);
            _currentHeading = new Heading(0.0);
            _currentSpeed = new Speed(10.0);
            _targetSpeed = new Speed(10.0);
            _targetSteering = SteeringAngle.Zero;
            _smoothedSteering = SteeringAngle.Zero;
            _stepDistance = 0.0;
        }

        public void Start(double lat, double lon, double headingDeg, double speedKmh)
        {
            lock (_lock)
            {
                _currentPosition = new Wgs84Position(lat, lon);
                _initialPosition = new Wgs84Position(lat, lon);  // Save for Reset() - legacy behavior
                _currentHeading = new Heading(headingDeg);
                _currentSpeed = new Speed(speedKmh);
                _targetSpeed = new Speed(speedKmh);
                _targetSteering = SteeringAngle.Zero;
                _smoothedSteering = SteeringAngle.Zero;
                _stepDistance = 0.0;
                IsEnabled = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                IsEnabled = false;
            }
        }

        public void SetSpeed(double speedKmh, bool smooth = false)
        {
            lock (_lock)
            {
                double clampedSpeed = Math.Clamp(speedKmh, -21.0, 322.0);
                var speed = new Speed(clampedSpeed);

                if (smooth)
                {
                    // Set target for gradual transition
                    _targetSpeed = speed;
                }
                else
                {
                    // Instant change
                    _currentSpeed = speed;
                    _targetSpeed = speed;
                }
            }
        }

        public void AdjustSpeed(double delta)
        {
            lock (_lock)
            {
                double newSpeed = Math.Clamp(_targetSpeed.KilometersPerHour + delta, -21.0, 322.0);
                _targetSpeed = new Speed(newSpeed);
            }
        }

        public void SetSpeedToZero()
        {
            lock (_lock)
            {
                _currentSpeed = new Speed(0.0);
                _targetSpeed = new Speed(0.0);
            }
        }

        public void SetSteering(double steerAngle)
        {
            lock (_lock)
            {
                _targetSteering = new SteeringAngle(steerAngle);
            }
        }

        public void ResetSteering()
        {
            lock (_lock)
            {
                _targetSteering = SteeringAngle.Zero;
                _smoothedSteering = SteeringAngle.Zero;
            }
        }

        public void ReverseDirection()
        {
            lock (_lock)
            {
                double newHeadingDeg = _currentHeading.Degrees + 180.0;
                if (newHeadingDeg >= 360.0) newHeadingDeg -= 360.0;
                _currentHeading = new Heading(newHeadingDeg);
            }
        }

        public void ResetPosition(double lat, double lon)
        {
            lock (_lock)
            {
                _currentPosition = new Wgs84Position(lat, lon);
            }
        }

        /// <summary>
        /// Reset simulator to initial start position (legacy FormGPS behavior).
        /// Does NOT clear speed, steering, or stop simulator - only resets position.
        /// Matches: btnResetSim_Click in FormGPS (Controls.Designer.cs:2157)
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _currentPosition = _initialPosition;
                // Legacy behavior: Does NOT clear speed, steering, or disable simulator
            }
        }

        /// <summary>
        /// Process a simulator event. Unified entry point for all simulator commands.
        /// </summary>
        public void ProcessEvent(AgOpenGPS.Api.Client.Commands.SimulatorEvent evt)
        {
            switch (evt.Type)
            {
                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.Start:
                    if (evt.StartData != null)
                    {
                        Start(
                            lat: evt.StartData.Position.Latitude,
                            lon: evt.StartData.Position.Longitude,
                            headingDeg: evt.StartData.Heading.Degrees,
                            speedKmh: evt.StartData.Speed.KilometersPerHour);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.Stop:
                    Stop();
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedAdjust:
                    if (evt.SpeedDelta.HasValue)
                    {
                        AdjustSpeed(evt.SpeedDelta.Value);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedSet:
                    if (evt.SpeedValue.HasValue)
                    {
                        SetSpeed(evt.SpeedValue.Value.KilometersPerHour, smooth: false);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedSetSmooth:
                    if (evt.SpeedValue.HasValue)
                    {
                        SetSpeed(evt.SpeedValue.Value.KilometersPerHour, smooth: true);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedZero:
                    SetSpeedToZero();
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SteeringSet:
                    if (evt.SteeringValue != null)
                    {
                        SetSteering(evt.SteeringValue.Degrees);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SteeringReset:
                    ResetSteering();
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.DirectionReverse:
                    ReverseDirection();
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.PositionReset:
                    if (evt.StartData != null)
                    {
                        ResetPosition(evt.StartData.Position.Latitude, evt.StartData.Position.Longitude);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.Reset:
                    Reset();
                    break;
            }
        }

        /// <summary>
        /// Simulation tick - updates position and generates GPS packet.
        /// Called by SimulatorHostedService timer (93ms).
        /// Returns PGN 0xD6 binary packet, or null if simulator disabled.
        /// </summary>
        public byte[]? Tick()
        {
            lock (_lock)
            {
                if (!IsEnabled)
                    return null;

                // Apply smooth speed transition
                double newSpeedKmh = _physics.TransitionSpeed(
                    _currentSpeed.KilometersPerHour,
                    _targetSpeed.KilometersPerHour,
                    ACCELERATION_RATE,
                    DECELERATION_RATE);
                _currentSpeed = new Speed(newSpeedKmh);

                // Smooth steering angle
                double newSmoothedSteeringDeg = _physics.SmoothSteeringAngle(
                    _smoothedSteering.Degrees,
                    _targetSteering.Degrees);
                _smoothedSteering = new SteeringAngle(newSmoothedSteeringDeg);

                // Calculate step distance from speed (93ms tick, speed in km/h)
                _stepDistance = (_currentSpeed.KilometersPerHour / 3600.0) * 0.093; // 93ms = 0.093 seconds

                // Update heading based on steering
                double headingChange = _physics.CalculateHeadingChange(_smoothedSteering.Degrees, _stepDistance);
                double headingRad = _currentHeading.ToRadians();
                headingRad += headingChange;

                // Normalize heading to [0, 2π)
                headingRad = _physics.NormalizeHeading(headingRad);
                _currentHeading = new Heading(headingRad * RAD_TO_DEG);

                // Update position using great circle navigation
                var (newLat, newLon) = _physics.CalculateNewPosition(
                    _currentPosition.Latitude,
                    _currentPosition.Longitude,
                    headingRad,
                    _stepDistance);
                _currentPosition = new Wgs84Position(newLat, newLon);

                // Generate PGN 0xD6 packet
                double altitude = _gnssGenerator.SimulateAltitude(_currentPosition.Latitude, _currentPosition.Longitude);

                return _serializer.EncodeGpsDataPacket(
                    _currentPosition.Latitude,
                    _currentPosition.Longitude,
                    _currentHeading.Degrees,
                    _currentSpeed.KilometersPerHour,
                    altitude,
                    _gnssGenerator.GetSatelliteCount(),
                    _gnssGenerator.GetFixQuality(),
                    _gnssGenerator.GetHdop(),
                    _gnssGenerator.GetAge());
            }
        }
    }
}
