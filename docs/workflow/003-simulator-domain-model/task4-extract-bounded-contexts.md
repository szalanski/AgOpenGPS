# Task 4: Extract Bounded Contexts into Separate Services

## Goal

Separate the three distinct bounded contexts currently mixed in SimulatorService into independent, testable classes with clear responsibilities.

## Steps

1. Extract SimulatorPhysics nested class into VehiclePhysicsService domain service
2. Create GnssDataGeneratorService to handle GPS-specific data generation (altitude, satellite count, quality)
3. Extract PacketEncoder nested class into AgIoProtocolSerializer infrastructure service
4. Define clear interfaces for each service (IVehiclePhysics, IGnssDataGenerator, IProtocolSerializer)
5. Refactor SimulatorService to compose these three services
6. Make nested classes internal and use InternalsVisibleTo for test assembly
7. Add unit tests for each bounded context in isolation

## Key Points

- Three distinct concerns currently conflated: vehicle physics, GPS generation, protocol encoding
- VehiclePhysicsService: speed transitions, steering smoothing, heading changes, position calculations
- GnssDataGeneratorService: altitude simulation, satellite data, HDOP/quality values
- AgIoProtocolSerializer: PGN 0xD6 packet structure, checksum calculation, byte encoding
- Each service should be independently testable without SimulatorService
- Services use value objects for all parameters and return values
- Consider dependency injection for future flexibility
- Keep in same files for now, just extract as separate classes (no folder reorganization)

## Acceptance

- [ ] VehiclePhysicsService exists with physics calculations
- [ ] GnssDataGeneratorService exists with GPS-specific logic
- [ ] AgIoProtocolSerializer exists with packet encoding
- [ ] Each service is testable in isolation
- [ ] SimulatorService composes these three services
- [ ] Interfaces defined for each bounded context
- [ ] Unit tests exist for each service
- [ ] Clear separation of concerns achieved

## Test

Write unit tests for each service independently - physics calculations, GPS data generation, and packet encoding should be testable without threading or SignalR.
