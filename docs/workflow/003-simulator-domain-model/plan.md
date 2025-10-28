# Simulator Domain Model Refactoring

## Goal

Refactor SimulatorService to follow Domain-Driven Design principles with strong typing, separated concerns, and rich behavior, while maintaining current functionality and test coverage.

## Current State

SimulatorService is a single service class with several design issues:
- **Primitive obsession**: All state stored as `double` fields (lat, lon, heading, speed, steering)
- **Anemic domain model**: Business logic in static utility methods within nested classes
- **Mixed concerns**: Three bounded contexts conflated (vehicle physics, GPS data generation, AgIO protocol serialization)
- **No type safety**: Invalid states possible (lat=999, heading=-500)
- **Infrastructure in domain**: Great circle navigation and packet encoding mixed with physics

## Target State

Simulator redesigned with proper DDD patterns:
- **Value objects**: Wgs84Position, Heading, Speed, SteeringAngle with invariant protection
- **Separated bounded contexts**: VehiclePhysics (domain), GnssDataGenerator (domain), AgIoProtocolSerializer (infrastructure)
- **Rich behavior**: Domain logic in methods, not static utilities
- **Type safety**: Compile-time prevention of invalid states
- **Clean boundaries**: Domain/application/infrastructure separation

## Why

**Type Safety**: Value objects enforce invariants at compile time - impossible to create invalid position (lat=999) or heading (500 degrees)

**Testability**: Pure domain logic separated from threading and infrastructure - can test physics calculations without SignalR or locks

**Maintainability**: Clear boundaries between vehicle simulation, GPS generation, and protocol serialization - easier to understand and modify

**Ubiquitous Language**: Code uses domain terminology (not "Ave", "stepDistance") matching expert vocabulary

## What This Is NOT

**Not adding persistence**: Simulator is ephemeral - no repository pattern, no event store, no state snapshots

**Not adding domain events**: Logging provides sufficient observability - no event bus, no event sourcing

**Not reorganizing folders**: Keep current project structure - no new layer folders or namespace changes

**Not adding aggregate identity**: Simulator is singleton service - no SimulatorId, no multiple instances

**Not adding features**: Same functionality - refactoring only, no new capabilities

**Not breaking external API**: SimulatorHostedService and integration tests remain compatible

## Migration Path

This refactoring follows the Strangler Fig pattern at the class level:

**Phase 1**: Create new value objects and bounded context classes alongside existing code

**Phase 2**: Update SimulatorService to use new types internally while maintaining public API

**Phase 3**: Remove old primitive-based code once fully migrated

**Phase 4**: Update tests to use new structure

Throughout migration, integration tests remain passing - functionality never breaks.

## Tasks

1. [task1-create-steering-angle-value-object.md](task1-create-steering-angle-value-object.md) - Create SteeringAngle value object with validation (completes value object set)
2. [task2-update-simulator-event-models.md](task2-update-simulator-event-models.md) - Update SimulatorStartData and SimulatorEvent to use value objects instead of primitives
3. [task3-refactor-simulator-service-internals.md](task3-refactor-simulator-service-internals.md) - Replace primitive fields with value objects throughout SimulatorService
4. [task4-extract-bounded-contexts.md](task4-extract-bounded-contexts.md) - Extract VehiclePhysics, GnssDataGenerator, AgIoProtocolSerializer as separate services
5. [task5-update-tests.md](task5-update-tests.md) - Refactor integration tests to use value objects and new structure

## Success Criteria

- [ ] All simulator state represented by value objects (no primitive fields)
- [ ] Invalid states impossible at compile time (type system enforces invariants)
- [ ] Three bounded contexts clearly separated in distinct classes
- [ ] Domain logic in rich methods, not static utilities
- [ ] All 21+ integration tests passing
- [ ] SimulatorHostedService works unchanged
- [ ] No performance regression (still 93ms tick rate)
- [ ] Test coverage maintained or improved
