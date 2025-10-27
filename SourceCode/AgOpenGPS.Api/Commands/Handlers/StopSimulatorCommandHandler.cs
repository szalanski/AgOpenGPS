using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class StopSimulatorCommandHandler : IRequestHandler<StopSimulatorCommand>
    {
        private readonly SimulatorService _simulator;

        public StopSimulatorCommandHandler(SimulatorService simulator)
        {
            _simulator = simulator;
        }

        public Task Handle(StopSimulatorCommand request, CancellationToken cancellationToken)
        {
            _simulator.Stop();
            return Task.CompletedTask;
        }
    }
}
