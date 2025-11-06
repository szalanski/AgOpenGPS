# Workflow 008: Local Plane Initialization Redesign and Strong Type Refactoring

## Goal

Eliminate initialization duplication, replace primitive obsession with strong types in simulator services, and add explicit origin control for field loading scenarios.

## Current State

- Local plane initialization duplicated in IGnssService and ICoordinateService
- SimulatorService and VehiclePhysicsService methods use primitive types (double lat, double lon, double speed, etc.)
- SimulatorStartData implicitly uses start position as coordinate origin (no override capability)
- UpdateSimulatorCommand requires verbose wrapping: `new UpdateSimulatorCommand(SimulatorEvent.Start(...))`
- Client cannot specify different origin when loading fields (Field.txt StartFix)
- Backend and frontend coordinate systems can become misaligned

## Target State

- Single responsibility: Only ICoordinateService manages local plane initialization
- All simulator service methods use strong types (Wgs84Position, Speed, Heading, SteeringAngle)
- SimulatorStartData accepts optional LocalPlaneOrigin parameter (defaults to start position)
- UpdateSimulatorCommand provides client-friendly factory methods
- Frontend can explicitly set origin when loading fields
- Backend and frontend coordinate systems stay synchronized

## Why

- **Single Responsibility**: Only ICoordinateService initializes local plane, not every service
- **Type Safety**: Strong types prevent unit confusion (degrees vs radians, km/h vs m/s)
- **Explicit Control**: Client can override origin for field loading scenarios
- **Clean API**: Factory methods hide SimulatorEvent wrapping complexity
- **Maintainability**: Value objects make code self-documenting and refactoring safer
- **Testability**: Strong types make test assertions clearer and catch bugs earlier

## What This Is NOT

- NOT adding new simulator features (physics, GNSS data generation)
- NOT changing coordinate transformation algorithms
- NOT modifying frontend field loading logic yet (backend preparation only)
- NOT breaking backward compatibility (optional parameters with defaults)
- NOT creating new value object types (reuse existing: Wgs84Position, Speed, Heading, SteeringAngle)

## Migration Path

### Phase 1: Initialization Refactoring (Already Complete)
- Remove InitializeLocalPlane from IGnssService interface
- GnssService injects and uses ICoordinateService
- SimulatorService handles initialization in ProcessEvent(Start)
- UpdateSimulatorCommandHandler becomes thin (no coordination)
- ApplicationOrchestrator removes placeholder initialization
- GnssService auto-initializes on first GPS fix (fallback)

### Phase 2: Strong Types (SimulatorService)
- Replace primitive parameters with value objects
- Update ProcessEvent to pass strong types directly
- Update all call sites (FormGPS, 37+ tests)

### Phase 3: Strong Types (VehiclePhysicsService)
- Replace primitive parameters with value objects
- Update return types to strong types
- SimulatorService.Tick() passes strong types

### Phase 4: Explicit Origin Control
- Add LocalPlaneOrigin to SimulatorStartData (optional, defaults to Position)
- Add GetEffectiveOrigin() helper method
- SimulatorService uses explicit origin or fallback
- Update logging to show explicit vs default

### Phase 5: Command Factory Methods
- Add static factory methods to UpdateSimulatorCommand
- Hide SimulatorEvent wrapping from client
- Update FormGPS and tests to use cleaner API

## Tasks

1. [task1-verify-initialization-refactoring.md](task1-verify-initialization-refactoring.md) - Verify initialization duplication removal works
2. [task2-simulator-service-strong-types.md](task2-simulator-service-strong-types.md) - Replace primitives in SimulatorService
3. [task3-vehicle-physics-strong-types.md](task3-vehicle-physics-strong-types.md) - Replace primitives in VehiclePhysicsService
4. [task4-explicit-local-plane-origin.md](task4-explicit-local-plane-origin.md) - Add LocalPlaneOrigin parameter
5. [task5-command-factory-methods.md](task5-command-factory-methods.md) - Add UpdateSimulatorCommand factories
6. [task6-update-call-sites.md](task6-update-call-sites.md) - Update FormGPS and tests
7. [task7-integration-testing.md](task7-integration-testing.md) - Verify all scenarios work

## Success Criteria

- [ ] All integration tests pass (41+ tests)
- [ ] No InitializeLocalPlane duplication (only in ICoordinateService)
- [ ] SimulatorService methods use strong types (no double lat/lon/speed/angle)
- [ ] VehiclePhysicsService methods use strong types (no primitive parameters)
- [ ] SimulatorStartData accepts optional LocalPlaneOrigin
- [ ] UpdateSimulatorCommand provides factory methods (Start, Stop, SetSpeed, etc.)
- [ ] FormGPS uses cleaner command API
- [ ] Backend can accept explicit origin from frontend (field loading ready)
- [ ] Coordinate system synchronization works in all scenarios
