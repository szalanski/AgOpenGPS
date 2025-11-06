# Task 1: Verify Initialization Refactoring

## Goal

Verify that the initialization duplication removal is correctly implemented in the code.

## Steps

1. Build AgOpenGPS.Api project to verify no compilation errors
2. Verify GnssService uses ICoordinateService (no internal CoordinateTransformer)
3. Verify SimulatorService initializes local plane on Start event
4. Verify ApplicationOrchestrator no longer has placeholder initialization
5. Verify UpdateSimulatorCommandHandler is thin (no coordination logic)
6. Review code to ensure only ICoordinateService has InitializeLocalPlane method

## Key Points

- GnssService should inject ICoordinateService in constructor
- SimulatorService.ProcessEvent(Start) should call coordinateService.InitializeLocalPlane()
- ApplicationOrchestrator.ExecuteAsync() should have comment explaining initialization happens elsewhere
- No duplication: only ICoordinateService has InitializeLocalPlane method
- Tests will be run at the end of workflow (Task 7)

## Acceptance

- [ ] AgOpenGPS.Api builds without errors
- [ ] IGnssService interface has no InitializeLocalPlane method
- [ ] GnssService injects ICoordinateService dependency
- [ ] SimulatorService initializes local plane in ProcessEvent(Start)
- [ ] UpdateSimulatorCommandHandler has no initialization logic
- [ ] ApplicationOrchestrator has no placeholder origin
- [ ] Code review confirms only ICoordinateService manages initialization
