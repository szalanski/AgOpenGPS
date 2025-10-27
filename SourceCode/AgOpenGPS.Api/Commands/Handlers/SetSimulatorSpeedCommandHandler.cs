using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class SetSimulatorSpeedCommandHandler : IRequestHandler<SetSimulatorSpeedCommand>
    {
        private readonly SimulatorService _simulator;

        public SetSimulatorSpeedCommandHandler(SimulatorService simulator)
        {
            _simulator = simulator;
        }

        public Task Handle(SetSimulatorSpeedCommand request, CancellationToken cancellationToken)
        {
            _simulator.SetSpeed(request.SpeedKmh);
            return Task.CompletedTask;
        }
    }
}
