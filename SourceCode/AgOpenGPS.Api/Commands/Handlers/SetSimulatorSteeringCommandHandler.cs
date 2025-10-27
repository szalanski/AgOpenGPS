using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AgOpenGPS.Api.Commands.Handlers
{
    public class SetSimulatorSteeringCommandHandler : IRequestHandler<SetSimulatorSteeringCommand>
    {
        private readonly SimulatorService _simulator;

        public SetSimulatorSteeringCommandHandler(SimulatorService simulator)
        {
            _simulator = simulator;
        }

        public Task Handle(SetSimulatorSteeringCommand request, CancellationToken cancellationToken)
        {
            _simulator.SetSteering(request.SteerAngle);
            return Task.CompletedTask;
        }
    }
}
