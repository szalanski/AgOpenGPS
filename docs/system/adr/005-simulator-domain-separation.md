# ADR 005: Simulator Domain Separation

## Status
**Accepted**

## Context

The simulator was initially implemented as **single monolithic service** (SimulatorService) containing:
- Vehicle physics (speed transitions, steering, turning, position calculation)
- GPS data generation (satellites, fix quality, HDOP, altitude)
- Binary protocol encoding (PGN 0xD6 packet serialization)

**Problems with monolithic approach**:
- Mixed concerns (physics + GPS + protocol in one class)
- Hard to understand (need to comprehend all three to change one)
- Hard to test (can't test physics without GPS or protocol)
- Hard to reuse (can't use physics without GPS logic)

## Decision

**Extract simulator into 3 bounded contexts** (Domain-Driven Design): VehiclePhysicsService (domain), GnssDataGenerator (domain), AgIoProtocolSerializer (infrastructure).

## Options Considered

### Option 1: Keep Monolithic
**Description**: Single SimulatorService with all logic

**Pros**:
- Fewer classes (simple)
- No coordination needed (all in one place)
- Fast (no method call overhead)

**Cons**:
- Mixed concerns (physics + GPS + protocol)
- Hard to understand (must read all to understand one)
- Hard to test (can't isolate physics from GPS)
- Hard to reuse (physics tied to GPS logic)

### Option 2: Layered Architecture
**Description**: Split into layers (Domain, Application, Infrastructure)

**Pros**:
- Clear layers (separation of concerns)
- Familiar pattern (many examples)

**Cons**:
- Horizontal slicing (layers cut across features)
- Doesn't match domain (physics and GPS are both domain, not different layers)
- Coupling across layers (changes ripple through layers)

### Option 3: Bounded Contexts (DDD) ✅ **CHOSEN**
**Description**: 3 bounded contexts with clear responsibilities

**Pros**:
- Clear boundaries (each context has single responsibility)
- Domain-aligned (matches expert mental model)
- Independently testable (test physics without GPS)
- Reusable (use physics in other simulators)
- Understandable (each context small, focused)

**Cons**:
- More classes (3 services instead of 1)
- Coordination needed (SimulatorService orchestrates)
- Slight overhead (method calls between services)

## Rationale

**Domain-Driven Design**: Bounded contexts match how domain experts think:
- "Vehicle physics" is distinct from "GPS signal characteristics"
- "Binary protocol encoding" is infrastructure (not domain)

**Single Responsibility Principle**: Each service has one reason to change:
- VehiclePhysicsService changes if vehicle dynamics change
- GnssDataGenerator changes if GPS simulation needs change
- AgIoProtocolSerializer changes if protocol format changes

**Testability**: Can test each context independently:
- Test physics: Given speed/steering, assert new position
- Test GPS: Assert satellite count in range, HDOP reasonable
- Test protocol: Encode/decode, assert round-trip correctness

**Reusability**: Services can be used elsewhere:
- VehiclePhysicsService → Guidance simulator (predict vehicle path)
- GnssDataGenerator → GPS receiver simulator
- AgIoProtocolSerializer → AgIO client implementation

**Maintainability**: Changes localize:
- Improve turn rate calculation → Only VehiclePhysicsService changes
- Add satellite masking → Only GnssDataGenerator changes
- Add IMU to protocol → Only AgIoProtocolSerializer changes

## Consequences

### Positive
- ✅ **Clear boundaries**: Each service has single, focused responsibility
- ✅ **Domain-aligned**: Services match expert mental models (physics, GPS, protocol)
- ✅ **Independently testable**: No mocking required (services don't depend on each other)
- ✅ **Reusable**: Use physics in guidance simulator, GPS generator elsewhere
- ✅ **Maintainable**: Changes localize (physics change doesn't affect GPS or protocol)
- ✅ **Understandable**: Each service small (~100-200 lines), easy to comprehend

### Negative
- ❌ **More classes**: 3 services instead of 1 (more files to navigate)
- ❌ **Coordination**: SimulatorService orchestrates (must know all three)
- ❌ **Method call overhead**: Slight performance cost (negligible at 93ms tick rate)

### Neutral
- ⚪ **Not microservices**: Services are in-process (not separate deployments)
- ⚪ **SimulatorService remains**: Orchestration layer (coordinates 3 services)

## Implementation

**Delivered in**: Workflow 003 (Simulator Domain Model Refactoring)

**Services created**:

**1. VehiclePhysicsService** (Domain)
- Speed transitions (acceleration, deceleration)
- Steering smoothing
- Heading changes (turn rate calculation)
- Position updates (great circle navigation)

**2. GnssDataGenerator** (Domain)
- Satellite count simulation (10-14 satellites)
- Fix quality (always RTK in simulator)
- HDOP (0.8 - excellent geometry)
- Altitude (terrain-based variation)
- Correction age (0.1s - fresh RTK data)

**3. AgIoProtocolSerializer** (Infrastructure)
- Binary encoding (PGN 0xD6 format)
- 128-byte packet structure
- Unit scaling (degrees × 100, etc.)
- Checksum (if required)

**SimulatorService orchestrates**:
```
Tick():
  Physics → Update speed, steering, heading, position
  GNSS → Get satellites, fix quality, HDOP, altitude
  Serializer → Encode to binary packet
  Return packet
```

**Commit reference**: See Workflow 003 task 4 (Extract bounded contexts)

**Date**: Workflow 003 completion (2025)

## Related Decisions

- **[006-value-object-modeling.md](006-value-object-modeling.md)**: Services use value objects (Wgs84Position, Heading, Speed)
- **[007-udp-communication-pattern.md](007-udp-communication-pattern.md)**: Simulator sends UDP (tests full pipeline including serializer)

## System Documentation

- **[../10-simulator-domain.md](../10-simulator-domain.md)**: How simulator bounded contexts work
- **[../06-simulator-capabilities.md](../06-simulator-capabilities.md)**: What simulator does (external view)

## References

- **Workflow 003 Plan**: [../../workflow/003-simulator-domain-model/plan.md](../../../workflow/003-simulator-domain-model/plan.md)
- **Domain-Driven Design**: Eric Evans, "Domain-Driven Design: Tackling Complexity in the Heart of Software"
- **Bounded Context pattern**: https://martinfowler.com/bliki/BoundedContext.html
- **Single Responsibility Principle**: https://en.wikipedia.org/wiki/Single-responsibility_principle
