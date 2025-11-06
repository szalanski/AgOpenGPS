# Task 6: Update Call Sites

## Goal

Update all call sites (FormGPS and integration tests) to use new strong type signatures and factory methods.

## Steps

1. Find all SimulatorEvent.Start calls (FormGPS and tests)
2. Update FormGPS.InitializeBackendConnection to use UpdateSimulatorCommand.Start factory
3. Update FormGPS to pass strong types (already uses Wgs84Position, Heading, Speed)
4. Find all test files that call SimulatorEvent.Start (37+ calls)
5. Update test calls to use UpdateSimulatorCommand.Start factory
6. Update tests that call SetSpeed, AdjustSpeed, SetSteering, etc. to use factories
7. Update tests that call VehiclePhysicsService methods to pass strong types
8. Update any tests that verify primitive behavior to verify strong type behavior
9. Run tests incrementally to catch compilation errors early
10. Verify no test behavior changes (only API changes)

## Key Points

- 37+ test call sites need updating (mostly in SimulatorIntegrationTests.cs)
- FormGPS has 1 call site in InitializeBackendConnection
- Tests may use primitive literals (convert to value objects: new Speed(10.0))
- Factory methods make test code cleaner and more readable
- Some tests may verify coordinate transformation (ensure origin logic unchanged)
- Run tests after each file update to catch errors early

## Acceptance

- [ ] FormGPS.InitializeBackendConnection uses UpdateSimulatorCommand.Start factory
- [ ] All SimulatorIntegrationTests use factory methods
- [ ] All StateReceptionTests use factory methods (if applicable)
- [ ] All tests pass with no behavior changes
- [ ] No SimulatorEvent wrapping visible in client code
- [ ] Test code is cleaner and more readable
- [ ] Code compiles without warnings
