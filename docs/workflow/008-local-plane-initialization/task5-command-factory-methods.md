# Task 5: Add UpdateSimulatorCommand Factory Methods

## Goal

Add client-friendly factory methods to UpdateSimulatorCommand to hide SimulatorEvent wrapping complexity.

## Steps

1. Add static Start factory method to UpdateSimulatorCommand
2. Add static Stop factory method to UpdateSimulatorCommand
3. Add static SetSpeed factory method to UpdateSimulatorCommand
4. Add static AdjustSpeed factory method to UpdateSimulatorCommand
5. Add static SetSteering factory method to UpdateSimulatorCommand
6. Add static ResetSteering factory method to UpdateSimulatorCommand
7. Add static ReverseDirection factory method to UpdateSimulatorCommand
8. Add static ResetPosition factory method to UpdateSimulatorCommand
9. Add static Reset factory method to UpdateSimulatorCommand
10. Each factory method internally creates SimulatorEvent and wraps in UpdateSimulatorCommand
11. Document factory methods with usage examples in code comments

## Key Points

- Factory methods match SimulatorEvent.Start(), SimulatorEvent.Stop(), etc. signatures
- Client code becomes cleaner: UpdateSimulatorCommand.Start(...) instead of new UpdateSimulatorCommand(SimulatorEvent.Start(...))
- SimulatorEvent remains internal implementation detail
- Factory methods use strong types from Task 2/3 (Wgs84Position, Speed, Heading, SteeringAngle)
- Start factory accepts optional LocalPlaneOrigin parameter (Task 4)
- No breaking changes (constructor still works for advanced scenarios)

## Acceptance

- [ ] UpdateSimulatorCommand has static Start factory method
- [ ] UpdateSimulatorCommand has static Stop factory method
- [ ] UpdateSimulatorCommand has static SetSpeed factory method
- [ ] UpdateSimulatorCommand has static AdjustSpeed factory method
- [ ] UpdateSimulatorCommand has static SetSteering factory method
- [ ] UpdateSimulatorCommand has static ResetSteering factory method
- [ ] UpdateSimulatorCommand has static ReverseDirection factory method
- [ ] UpdateSimulatorCommand has static ResetPosition factory method
- [ ] UpdateSimulatorCommand has static Reset factory method
- [ ] All factory methods use strong types
- [ ] Code comments document usage pattern
