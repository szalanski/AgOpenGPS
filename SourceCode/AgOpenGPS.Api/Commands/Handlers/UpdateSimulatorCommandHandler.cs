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
            _logger.LogDebug("Processing simulator event: {EventType}", request.Event.Type);

            // Delegate to service for business logic
            _simulator.ProcessEvent(request.Event);

            return Task.CompletedTask;
        }
    }
}
