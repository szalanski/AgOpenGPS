using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Client.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class UpdateLocalPlaneCommandHandler : IRequestHandler<UpdateLocalPlaneCommand>
    {
        private readonly ICoordinateService _coordinateService;
        private readonly ILogger<UpdateLocalPlaneCommandHandler> _logger;

        public UpdateLocalPlaneCommandHandler(
            ICoordinateService coordinateService,
            ILogger<UpdateLocalPlaneCommandHandler> logger)
        {
            _coordinateService = coordinateService;
            _logger = logger;
        }

        public Task Handle(UpdateLocalPlaneCommand request, CancellationToken cancellationToken)
        {
            // Single method - no conditional logic needed
            _coordinateService.SetOrigin(request.Origin);

            _logger.LogInformation("Local plane origin set to: {Lat:F6}, {Lon:F6}",
                request.Origin.Latitude, request.Origin.Longitude);

            return Task.CompletedTask;
        }
    }
}
