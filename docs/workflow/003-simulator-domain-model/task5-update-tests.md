# Task 5: Update Tests to Use New Domain Model

## Goal

Refactor integration and unit tests to use value objects and new bounded context services while maintaining or improving test coverage.

## Steps

1. Update integration tests to create SimulatorEvent instances with value objects
2. Refactor test assertions to compare value objects instead of primitives
3. Add unit tests for each bounded context service (VehiclePhysics, GnssDataGenerator, ProtocolSerializer)
4. Add unit tests for value objects (SteeringAngle validation and behavior)
5. Verify all 21+ existing integration tests still pass
6. Add new tests for edge cases now prevented by type system
7. Update test helper methods (GpsSimulator) to use value objects
8. Measure and maintain code coverage

## Key Points

- SimulatorIntegrationTests currently uses primitive doubles for setup
- Update to use Wgs84Position, Heading, Speed, SteeringAngle throughout
- Test helper GpsSimulator may need value object support
- Add unit tests for new bounded context services in isolation
- Tests should demonstrate type safety benefits (invalid states impossible)
- Consider adding property-based tests for value object invariants
- Integration tests verify end-to-end functionality unchanged
- Unit tests verify bounded contexts work correctly in isolation

## Acceptance

- [ ] All integration tests updated to use value objects
- [ ] All 21+ integration tests passing
- [ ] Unit tests added for VehiclePhysicsService
- [ ] Unit tests added for GnssDataGeneratorService
- [ ] Unit tests added for AgIoProtocolSerializer
- [ ] Unit tests added for SteeringAngle value object
- [ ] Test coverage maintained or improved
- [ ] No flaky tests introduced

## Test

Run full test suite with dotnet test - verify all tests pass and coverage meets or exceeds previous levels.
