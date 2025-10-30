using AgOpenGPS.Api.Abstractions;
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
    /// Delegates to SimulatorService.ProcessEvent() for business logic.
    /// CRITICAL: Initializes local plane on Start to prevent coordinate jitter.
    /// </summary>
    public class UpdateSimulatorCommandHandler : IRequestHandler<UpdateSimulatorCommand>
    {
        private readonly SimulatorService _simulator;
        private readonly IGnssService _gnssService;
        private readonly ICoordinateService _coordinateService;
        private readonly ILogger<UpdateSimulatorCommandHandler> _logger;

        public UpdateSimulatorCommandHandler(
            SimulatorService simulator,
            IGnssService gnssService,
            ICoordinateService coordinateService,
            ILogger<UpdateSimulatorCommandHandler> logger)
        {
            _simulator = simulator;
            _gnssService = gnssService;
            _coordinateService = coordinateService;
            _logger = logger;
        }

        public Task Handle(UpdateSimulatorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing simulator event: {EventType}", request.Event.Type);

            // CRITICAL FIX: Initialize local plane when simulator starts
            // This prevents coordinate jitter from using placeholder origin (45.0, -93.0)
            // See docs/backend-simulator-shaking.md for detailed explanation
            if (request.Event.Type == SimulatorEventType.Start && request.Event.StartData != null)
            {
                var origin = request.Event.StartData.Position;
                _gnssService.InitializeLocalPlane(origin);
                _coordinateService.InitializeLocalPlane(origin);

                _logger.LogInformation("Local plane initialized to simulator start position: ({Lat:F6}, {Lon:F6})",
                    origin.Latitude, origin.Longitude);
            }

            // Delegate to service for business logic
            _simulator.ProcessEvent(request.Event);

            return Task.CompletedTask;
        }
    }
}
