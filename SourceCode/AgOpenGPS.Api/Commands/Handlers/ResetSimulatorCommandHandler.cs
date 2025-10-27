using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class ResetSimulatorCommandHandler : IRequestHandler<ResetSimulatorCommand>
    {
        private readonly SimulatorService _simulator;

        public ResetSimulatorCommandHandler(SimulatorService simulator)
        {
            _simulator = simulator;
        }

        public Task Handle(ResetSimulatorCommand request, CancellationToken cancellationToken)
        {
            _simulator.Reset();
            return Task.CompletedTask;
        }
    }
}
