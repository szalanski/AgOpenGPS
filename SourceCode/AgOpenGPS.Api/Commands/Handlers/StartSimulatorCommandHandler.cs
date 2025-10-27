using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class StartSimulatorCommandHandler : IRequestHandler<StartSimulatorCommand>
    {
        private readonly SimulatorService _simulator;

        public StartSimulatorCommandHandler(SimulatorService simulator)
        {
            _simulator = simulator;
        }

        public Task Handle(StartSimulatorCommand request, CancellationToken cancellationToken)
        {
            _simulator.Start(
                request.Latitude,
                request.Longitude,
                request.HeadingDegrees,
                request.SpeedKmh);

            return Task.CompletedTask;
        }
    }
}
