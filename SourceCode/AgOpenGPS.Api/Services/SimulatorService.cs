using System;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Domain.Simulator;
using Microsoft.Extensions.Logging;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// Application service orchestrating the simulator aggregate and related infrastructure components.
    /// </summary>
    public class SimulatorService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(93);
        private readonly object _lock = new();

        private readonly ILogger<SimulatorService> _logger;
        private readonly ICoordinateService _coordinateService;
        private readonly VehiclePhysicsDomainService _physics;
        private readonly GnssDataGenerator _gnssGenerator;
        private readonly AgIoProtocolSerializer _serializer;
        private readonly SimulatorAggregate _aggregate;

        public bool IsEnabled => _aggregate.IsEnabled;

        public SteeringAngle GetCurrentSteering() => _aggregate.SmoothedSteering;

        public SimulatorService(
            ILogger<SimulatorService> logger,
            ICoordinateService coordinateService,
            VehiclePhysicsDomainService physics,
            GnssDataGenerator gnssGenerator,
            AgIoProtocolSerializer serializer)
        {
            _logger = logger;
            _coordinateService = coordinateService;
            _physics = physics;
            _gnssGenerator = gnssGenerator;
            _serializer = serializer;
            _aggregate = new SimulatorAggregate();
        }

        public void Start(Wgs84Position position, Heading heading, Speed speed)
        {
            lock (_lock)
            {
                _aggregate.Start(position, heading, speed);
                _logger.LogInformation(
                    "Simulator STARTED: Position=({Lat:F6}, {Lon:F6}), Heading={Heading:F1} deg, Speed={Speed:F1} km/h",
                    _aggregate.CurrentPosition.Latitude,
                    _aggregate.CurrentPosition.Longitude,
                    _aggregate.CurrentHeading.Degrees,
                    _aggregate.CurrentSpeed.KilometersPerHour);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _aggregate.Stop();
                _logger.LogInformation("Simulator STOPPED");
            }
        }

        public void SetSpeed(Speed speed, bool smooth = false)
        {
            lock (_lock)
            {
                if (smooth)
                {
                    _aggregate.SetTargetSpeed(speed);
                    _logger.LogDebug(
                        "Speed set (smooth): Target={Target:F1} km/h (current={Current:F1} km/h)",
                        _aggregate.TargetSpeed.KilometersPerHour,
                        _aggregate.CurrentSpeed.KilometersPerHour);
                }
                else
                {
                    _aggregate.SetInstantSpeed(speed);
                    _logger.LogDebug("Speed set (instant): {Speed:F1} km/h", _aggregate.CurrentSpeed.KilometersPerHour);
                }
            }
        }

        public void AdjustSpeed(double deltaKph)
        {
            lock (_lock)
            {
                _aggregate.AdjustTargetSpeed(deltaKph);
                _logger.LogDebug("Speed adjusted: {NewSpeed:F1} km/h", _aggregate.TargetSpeed.KilometersPerHour);
            }
        }

        public void SetSpeedToZero()
        {
            lock (_lock)
            {
                _aggregate.ZeroSpeed();
                _logger.LogDebug("Speed set to ZERO");
            }
        }

        public void SetSteering(SteeringAngle steeringAngle)
        {
            lock (_lock)
            {
                _aggregate.SetTargetSteering(steeringAngle);
                _logger.LogDebug("Steering set: {Angle:F1} deg", _aggregate.TargetSteering.Degrees);
            }
        }

        public void ResetSteering()
        {
            lock (_lock)
            {
                _aggregate.ResetSteering();
                _logger.LogDebug("Steering RESET to 0 deg");
            }
        }

        public void ReverseDirection()
        {
            lock (_lock)
            {
                _aggregate.ReverseDirection();
                _logger.LogDebug("Direction REVERSED: {Heading:F1} deg", _aggregate.CurrentHeading.Degrees);
            }
        }

        public void ResetPosition(Wgs84Position position)
        {
            lock (_lock)
            {
                _aggregate.ResetPosition(position);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _aggregate.ResetToInitialPosition();
                _logger.LogDebug(
                    "Position RESET to initial: ({Lat:F6}, {Lon:F6})",
                    _aggregate.InitialPosition.Latitude,
                    _aggregate.InitialPosition.Longitude);
            }
        }

        public void ProcessEvent(AgOpenGPS.Api.Client.Commands.SimulatorEvent evt)
        {
            _logger.LogInformation("SimulatorEvent received: {EventType}", evt.Type);

            switch (evt.Type)
            {
                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.Start:
                    if (evt.StartData != null)
                    {
                        var effectiveOrigin = evt.StartData.GetEffectiveOrigin();
                        _coordinateService.InitializeLocalPlane(effectiveOrigin);

                        bool isExplicitOrigin = evt.StartData.LocalPlaneOrigin.HasValue;
                        if (isExplicitOrigin)
                        {
                            _logger.LogInformation(
                                "Local plane initialized with explicit origin: ({Lat:F6}, {Lon:F6})",
                                effectiveOrigin.Latitude,
                                effectiveOrigin.Longitude);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Local plane initialized with default origin (start position): ({Lat:F6}, {Lon:F6})",
                                effectiveOrigin.Latitude,
                                effectiveOrigin.Longitude);
                        }

                        Start(evt.StartData.Position, evt.StartData.Heading, evt.StartData.Speed);
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
                        SetSpeed(evt.SpeedValue.Value, smooth: false);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedSetSmooth:
                    if (evt.SpeedValue.HasValue)
                    {
                        SetSpeed(evt.SpeedValue.Value, smooth: true);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SpeedZero:
                    SetSpeedToZero();
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.SteeringSet:
                    if (evt.SteeringValue != null)
                    {
                        SetSteering(evt.SteeringValue);
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
                        ResetPosition(evt.StartData.Position);
                    }
                    break;

                case AgOpenGPS.Api.Client.Commands.SimulatorEventType.Reset:
                    Reset();
                    break;
            }
        }

        /// <summary>
        /// Simulation tick - updates position and generates GPS packet.
        /// </summary>
        public byte[]? Tick()
        {
            lock (_lock)
            {
                if (!_aggregate.IsEnabled)
                {
                    _logger.LogDebug("Tick called but simulator is DISABLED");
                    return null;
                }

                _aggregate.AdvanceTick(TickInterval, _physics);

                double altitude = _gnssGenerator.SimulateAltitude(
                    _aggregate.CurrentPosition.Latitude,
                    _aggregate.CurrentPosition.Longitude);

                return _serializer.EncodeGpsDataPacket(
                    _aggregate.CurrentPosition.Latitude,
                    _aggregate.CurrentPosition.Longitude,
                    _aggregate.CurrentHeading.Degrees,
                    _aggregate.CurrentSpeed.KilometersPerHour,
                    altitude,
                    _gnssGenerator.GetSatelliteCount(),
                    _gnssGenerator.GetFixQuality(),
                    _gnssGenerator.GetHdop(),
                    _gnssGenerator.GetAge());
            }
        }
    }
}
