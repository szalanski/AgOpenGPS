using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    /// <summary>
    /// Unified handler for all simulator commands.
    /// Uses lenient error handling - missing values are silently skipped with warnings.
    /// </summary>
    public class UpdateSimulatorCommandHandler : IRequestHandler<UpdateSimulatorCommand>
    {
        private readonly SimulatorService _simulator;
        private readonly ILogger<UpdateSimulatorCommandHandler> _logger;

        public UpdateSimulatorCommandHandler(
            SimulatorService simulator,
            ILogger<UpdateSimulatorCommandHandler> logger)
        {
            _simulator = simulator;
            _logger = logger;
        }

        public Task Handle(UpdateSimulatorCommand request, CancellationToken cancellationToken)
        {
            var evt = request.Event;

            switch (evt.Type)
            {
                case SimulatorEventType.Start:
                    if (evt.StartData != null)
                    {
                        _simulator.Start(
                            lat: evt.StartData.Latitude,
                            lon: evt.StartData.Longitude,
                            headingDeg: evt.StartData.Heading,
                            speedKmh: evt.StartData.Speed);
                        _logger.LogInformation("Simulator started at ({Lat}, {Lon}), heading={Heading}°, speed={Speed} km/h",
                            evt.StartData.Latitude, evt.StartData.Longitude, evt.StartData.Heading, evt.StartData.Speed);
                    }
                    else
                    {
                        _logger.LogWarning("Start event received without StartData - ignoring");
                    }
                    break;

                case SimulatorEventType.Stop:
                    _simulator.Stop();
                    _logger.LogInformation("Simulator stopped");
                    break;

                case SimulatorEventType.SpeedAdjust:
                    if (evt.Value.HasValue)
                    {
                        _simulator.AdjustSpeed(evt.Value.Value);
                        _logger.LogDebug("Speed adjusted by {Delta} km/h", evt.Value.Value);
                    }
                    else
                    {
                        _logger.LogWarning("SpeedAdjust event without Value - ignoring");
                    }
                    break;

                case SimulatorEventType.SpeedSet:
                    if (evt.Value.HasValue)
                    {
                        _simulator.SetSpeed(evt.Value.Value, smooth: false);
                        _logger.LogDebug("Speed set to {Speed} km/h (instant)", evt.Value.Value);
                    }
                    else
                    {
                        _logger.LogWarning("SpeedSet event without Value - ignoring");
                    }
                    break;

                case SimulatorEventType.SpeedSetSmooth:
                    if (evt.Value.HasValue)
                    {
                        _simulator.SetSpeed(evt.Value.Value, smooth: true);
                        _logger.LogDebug("Speed set to {Speed} km/h (smooth transition)", evt.Value.Value);
                    }
                    else
                    {
                        _logger.LogWarning("SpeedSetSmooth event without Value - ignoring");
                    }
                    break;

                case SimulatorEventType.SpeedZero:
                    _simulator.SetSpeedToZero();
                    _logger.LogDebug("Speed set to zero (instant stop)");
                    break;

                case SimulatorEventType.SteeringSet:
                    if (evt.Value.HasValue)
                    {
                        _simulator.SetSteering(evt.Value.Value);
                        _logger.LogDebug("Steering set to {Angle}°", evt.Value.Value);
                    }
                    else
                    {
                        _logger.LogWarning("SteeringSet event without Value - ignoring");
                    }
                    break;

                case SimulatorEventType.SteeringReset:
                    _simulator.ResetSteering();
                    _logger.LogDebug("Steering reset to center (0°)");
                    break;

                case SimulatorEventType.DirectionReverse:
                    _simulator.ReverseDirection();
                    _logger.LogInformation("Direction reversed (180°)");
                    break;

                case SimulatorEventType.PositionReset:
                    if (evt.StartData != null)
                    {
                        _simulator.ResetPosition(evt.StartData.Latitude, evt.StartData.Longitude);
                        _logger.LogInformation("Position reset to ({Lat}, {Lon})",
                            evt.StartData.Latitude, evt.StartData.Longitude);
                    }
                    else
                    {
                        // No-op if no position provided - silently skip
                        _logger.LogDebug("PositionReset without coordinates - ignoring");
                    }
                    break;

                case SimulatorEventType.Reset:
                    _simulator.Reset();
                    _logger.LogInformation("Simulator fully reset");
                    break;

                default:
                    _logger.LogWarning("Unknown simulator event type: {Type}", evt.Type);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
